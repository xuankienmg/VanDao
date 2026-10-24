using VanDao.Data;

namespace VanDao.Game;

/// <summary>
/// Lôi đài giả để thử trong môi trường dev (ArenaService.Mock): 18 đối thủ Hóa Thần – Luyện Hư mặc đồ thật của cảnh giới đó,
/// trận tính bằng ArenaService.Duel (cùng công thức SQL), thắng thì đổi vị trí như bản thật.
/// Chỉ nằm trong bộ nhớ: tắt game là mất, không đụng Supabase.
/// </summary>
public sealed class ArenaMock
{
    // luật như arena_rules() nhưng nới lượt để thử thoải mái
    private const int MockDaily = 99, MinWindow = 5, WindowPct = 20, MaxRounds = 30;

    private sealed class Entry
    {
        public Guid Id; public string Name = ""; public int Realm, Stage; public CombatStats Stats = new(1, 1, 1);
        public int Wins, Losses;
    }

    private static readonly string[] Names =
    [
        "Hàn Lập", "Mặc Đại Phu", "Nam Cung Uyển", "Lệ Phi Vũ", "Tử Linh", "Nguyên Dao", "Lý Hóa Nguyên", "Ôn Thiên Nhân", "Bạch Dao Di",
        "Hướng Chi Lễ", "Thiên Lan Thánh Nữ", "Bảo Hoa", "Cực Âm Tổ Sư", "Vạn Thiên Minh", "Lục Diệp Tôn Giả", "Phong Hi", "Ngân Nguyệt", "Thanh Dịch Cư Sĩ",
    ];

    private readonly object _gate = new();
    private readonly List<Entry> _ladder = [];          // vị trí = chỉ số + 1
    private readonly List<ArenaService.HistoryRow> _history = [];
    private int _used; private long _nextId = 1;

    public ArenaMock()
    {
        var rng = new Random(20261024);
        var i = 0;
        foreach (var realm in new[] { 5, 4 })            // Luyện Hư, Hóa Thần
            for (var stage = GameData.Realms[realm].Stages.Length - 1; stage >= 0; stage--)
                for (var k = 0; k < 3; k++)
                    _ladder.Add(new Entry { Id = new Guid(i + 1, 0x4d4f, 0x434b, 0, 0, 0, 0, 0, 0, 0, (byte)(i + 1)), Name = Names[i++], Realm = realm, Stage = stage, Stats = FakeStats(realm, stage, rng) });
        _ladder.Sort((a, b) => Power(b.Stats).CompareTo(Power(a.Stats)));
        foreach (var e in _ladder) { e.Wins = rng.Next(0, 30); e.Losses = rng.Next(0, 20); }
    }

    /// <summary>Chỉ số gốc của cảnh giới / tầng + một món đồ thật cho mỗi ô (phẩm Linh → Địa ngẫu nhiên), bạo kích / thân pháp như người mặc đồ đó.</summary>
    private static CombatStats FakeStats(int realm, int stage, Random rng)
    {
        var r = GameData.Realms[realm];
        var b = GameData.BaseStats(r, stage);
        int atk = b.Atk, hp = b.Hp, def = b.Def; double crit = 5, speed = GameData.BaseSpeed[Math.Min(realm, GameData.BaseSpeed.Length - 1)];
        foreach (var slot in r.EquipSlots)
        {
            var pool = GameData.AllItems.Where(x => x.Slot == slot && x.MinRealm == realm
                && Array.IndexOf(GameData.RarityOrder, x.Rarity) is >= 1 and <= 3).ToList();
            if (pool.Count == 0) continue;
            var it = pool[rng.Next(pool.Count)];
            atk += it.Atk; hp += it.Hp; def += it.Def; crit += it.CritPct; speed += it.SpeedPct;
        }
        var talent = 1 + rng.NextDouble() * .25;           // công pháp / linh thú: +0–25%
        return new((int)(atk * talent), (int)(hp * talent), (int)(def * talent), Math.Min(60, speed), Math.Min(100, crit));
    }

    private static long Power(CombatStats s) => (long)Math.Round(s.Atk * 2.0 + s.Def * 3.0 + s.Hp * .4 + (s.Crit + s.Speed) * 4);
    private static long Window(long rank) => Math.Max(MinWindow, (long)Math.Ceiling(rank * WindowPct / 100.0));

    /// <summary>Đưa người chơi vào cuối bậc thang (lần đầu) và cập nhật tên / cảnh giới / chỉ số hiện tại.</summary>
    private Entry Me(Player p)
    {
        var me = _ladder.FirstOrDefault(e => e.Id == p.Id);
        if (me is null) { me = new Entry { Id = p.Id }; _ladder.Add(me); }
        me.Name = p.Name; me.Realm = p.Realm; me.Stage = p.Stage; me.Stats = GameService.Stats(p);
        return me;
    }

    private ArenaService.Row Row(Entry e) => new(e.Id, e.Name, e.Realm, e.Stage, GameData.Realms[e.Realm].Name,
        GameData.Realms[e.Realm].Stages[Math.Clamp(e.Stage, 0, GameData.Realms[e.Realm].Stages.Length - 1)], Power(e.Stats), e.Wins, e.Losses, _ladder.IndexOf(e) + 1);

    public ArenaService.StateRow State(Player p)
    {
        lock (_gate)
        {
            var me = Me(p);
            long rank = _ladder.IndexOf(me) + 1, w = Window(rank);
            return new ArenaService.StateRow(Row(me), _ladder.Take(3).Select(Row).ToList(),
                _ladder.Select(Row).Where(x => x.Rank > 3 && x.Rank >= rank - w && x.Rank <= rank + 2).ToList(),
                w, _used, MockDaily, null, _ladder.Count, _history.AsEnumerable().Reverse().Take(20).ToList());
        }
    }

    public ArenaService.FightRow Fight(Player p, Guid target)
    {
        lock (_gate)
        {
            if (target == p.Id) throw new GameException("Không thể tự khiêu chiến chính mình.");
            if (_used >= MockDaily) throw new GameException($"Đã dùng hết {MockDaily} lượt khiêu chiến hôm nay. Làm mới lúc 0h.");
            var me = Me(p);
            var foe = _ladder.FirstOrDefault(e => e.Id == target) ?? throw new GameException("Đối thủ không còn ở Lôi đài.");
            int mi = _ladder.IndexOf(me), fi = _ladder.IndexOf(foe);
            long myRank = mi + 1, foeRank = fi + 1;
            if (foeRank >= myRank) throw new GameException("Chỉ khiêu chiến được người xếp trên bạn.");
            if (myRank - foeRank > Window(myRank)) throw new GameException($"Đối thủ ngoài tầm: chỉ khiêu chiến được trong {Window(myRank)} hạng phía trên.");
            var seed = Random.Shared.NextInt64(2147483647);
            var d = ArenaService.Duel(me.Stats, foe.Stats, seed, MaxRounds);
            if (d.win) { (_ladder[mi], _ladder[fi]) = (foe, me); me.Wins++; foe.Losses++; }
            else { me.Losses++; foe.Wins++; }
            _used++;
            var id = _nextId++;
            _history.Add(new ArenaService.HistoryRow(id, true, foe.Name, d.win, myRank, d.win ? foeRank : myRank, DateTime.UtcNow));
            return new ArenaService.FightRow(d.win, d.ko, d.maxA, d.maxD, d.hpA, d.hpD, d.turns, id, foe.Name, foe.Realm, myRank, d.win ? foeRank : myRank, foeRank);
        }
    }
}
