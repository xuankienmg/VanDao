using Microsoft.EntityFrameworkCore;
using VanDao.Data;

namespace VanDao.Game;

/// <summary>
/// Toàn bộ luật chơi chạy ở server. Client chỉ hiển thị và gửi hành động,
/// nên người chơi không thể sửa tu vi hay vật phẩm từ trình duyệt.
/// </summary>
public class GameService(GameDbContext db)
{
    private const int LogKeep = 100;
    private const int LogShow = 40;

    // ================= Truy vấn =================

    public async Task<Player> CreateAsync(string? name, Guid? id = null)
    {
        var p = new Player { Id = id ?? Guid.NewGuid(), Name = string.IsNullOrWhiteSpace(name) ? "Đạo hữu" : name.Trim()[..Math.Min(64, name.Trim().Length)] };
        foreach (var (item, n) in GameData.StartingInventory) p.Add(item, n);
        db.Players.Add(p);
        p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
        AddLog(p, "Đạo tâm vừa khởi. Nhân vật sẽ tự đi thám hiểm, nhặt linh tài và tích tu vi.");
        await db.SaveChangesAsync();
        return p;
    }

    /// <summary>
    /// Tu sĩ đang chơi trên máy này: người có tiến độ cao nhất (cảnh giới, tầng, số vật phẩm),
    /// hoà thì lấy người chơi gần nhất; chưa có thì tạo mới.
    /// Không dựa vào localStorage vì cửa sổ desktop chạy server ở cổng ngẫu nhiên mỗi lần mở
    /// (bản cũ vì vậy đã lỡ tạo thêm các tu sĩ trống — cách chọn này lấy lại đúng nhân vật thật).
    /// </summary>
    public async Task<Player> CurrentAsync()
    {
        var id = await db.Players
            .Select(x => new { x.Id, x.Realm, x.Stage, Items = x.Inventory.Sum(i => i.Count), x.LastTickAt })
            .OrderByDescending(x => x.Realm).ThenByDescending(x => x.Stage).ThenByDescending(x => x.Items).ThenByDescending(x => x.LastTickAt)
            .Select(x => x.Id).FirstOrDefaultAsync();
        return id == Guid.Empty ? await CreateAsync(null) : await LoadAsync(id);
    }

    /// <summary>Nạp người chơi và cộng tu vi theo thời gian thực đã trôi qua.</summary>
    public async Task<Player> LoadAsync(Guid id)
    {
        var p = await db.Players.Include(x => x.Inventory).Include(x => x.Equipment).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException();
        Tick(p, DateTime.UtcNow);
        // ô trang bị chưa mở ở cảnh giới hiện tại (dữ liệu bản cũ) → trả món về túi
        foreach (var e in p.Equipment.Where(e => !Realm(p).EquipSlots.Contains(e.Slot)).ToList())
        {
            p.Add(e.ItemId, 1);
            p.Equipment.Remove(e);
            db.Equipment.Remove(e);
        }
        return p;
    }

    /// <summary>Số ô túi đang dùng: mỗi món trang bị một ô, mỗi loại vật phẩm một ô.</summary>
    public static int BagUsed(Player p) =>
        p.Inventory.Where(i => i.Count > 0).Sum(i => GameData.ItemById.TryGetValue(i.ItemId, out var d) && d.Slot is not null ? i.Count : 1);

    public static int BagCap(Player p) => Realm(p).BagSlots;

    /// <summary>Thêm vật phẩm rơi vào túi nếu còn chỗ; trả về số lượng thực nhận.</summary>
    private static int TryAddLoot(Player p, string itemId, int n)
    {
        var free = BagCap(p) - BagUsed(p);
        var isGear = GameData.ItemById[itemId].Slot is not null;
        if (isGear) n = Math.Max(0, Math.Min(n, free));
        else if (p.Count(itemId) == 0 && free <= 0) n = 0;   // loại vật phẩm mới cần thêm một ô
        if (n > 0) p.Add(itemId, n);
        return n;
    }

    public async Task<StateDto> BuildStateAsync(Player p)
    {
        var logs = await db.Logs.Where(l => l.PlayerId == p.Id)
            .OrderByDescending(l => l.Id).Take(LogShow)
            .Select(l => new LogDto(DateTime.SpecifyKind(l.At, DateTimeKind.Utc), l.Text, l.Kind))
            .ToListAsync();
        // log mới thêm trong request này chưa lưu thì chưa có trong DB
        logs.InsertRange(0, p.Logs.Where(l => l.Id == 0).OrderByDescending(l => l.At)
            .Select(l => new LogDto(l.At, l.Text, l.Kind)));

        var r = Realm(p);
        var peak = IsPeak(p);
        var next = NextRealm(p);
        var cost = TargetCost(p);

        string title, kind;
        if (peak && next is null) { title = $"{r.Name} {r.Stages[p.Stage]}"; kind = "Đỉnh phong hiện tại"; }
        else if (peak) { title = $"{next!.Name} · {next.Stages[0]}"; kind = "Đột phá cảnh giới"; }
        else { title = $"{r.Name} · {r.Stages[p.Stage + 1]}"; kind = "Lên tầng tiếp theo"; }

        return new StateDto(
            p.Id, p.Name, p.Realm, p.Stage, r.Name, r.Stages[p.Stage],
            p.Qi, r.QiReq(p.Stage), Rate(p), Rate(p) * GameData.ChannelMultiplier,
            peak, next is not null, title, kind,
            cost.Select(kv => new CostDto(kv.Key, kv.Value, p.Count(kv.Key))).ToList(),
            Chance(p), p.Bonus,
            Math.Max(0, (long)(p.ExploreReadyAt - DateTime.UtcNow).TotalMilliseconds),
            p.AutoExplore,
            (long)(ExploreSeconds(p) * 1000),
            ExploreQi(p),
            p.Equipment.ToDictionary(e => e.Slot, e => e.ItemId),
            new BonusDto(Bonus(p, i => i.QiPct), Bonus(p, i => i.LuckPct), Bonus(p, i => i.SpeedPct)),
            Stats(p),
            Battle(p),
            r.Cn, next?.Name ?? GameData.LockedRealms[0], next?.Cn ?? GameData.LockedRealmsCn[0], next is null && peak,
            BagUsed(p), BagCap(p), r.EquipSlots, Level(p),
            p.Inventory.Where(i => i.Count > 0).ToDictionary(i => i.ItemId, i => i.Count),
            GameData.Recipes.Select((rc, i) => new RecipeStateDto(i, p.Realm < rc.MinRealm, p.Realm >= rc.MinRealm && p.Has(rc.In))).ToList(),
            logs.Take(LogShow).ToList());
    }

    // ================= Hành động =================

    public string Channel(Player p)
    {
        var gain = Rate(p) * GameData.ChannelMultiplier;
        p.Qi = Math.Min(QiReq(p), p.Qi + gain);
        return $"+{gain:0.#} tu vi";
    }

    public (string msg, string kind) Equip(Player p, string itemId, string? targetSlot = null)
    {
        if (!GameData.ItemById.TryGetValue(itemId, out var def) || def.Slot is null) throw new GameException("Vật phẩm này không trang bị được.");
        if (p.Count(itemId) < 1) throw new GameException("Không có vật phẩm này trong hành trang.");
        if (p.Realm < def.MinRealm) throw new GameException($"Cần cảnh giới {GameData.Realms[def.MinRealm].Name} để mặc {def.Name}.");
        // nhẫn đeo được ở Nhẫn 1 hoặc Nhẫn 2; các món khác chỉ có một ô
        var options = def.Slot == "ring" ? new[] { "ring", "ring2" } : new[] { def.Slot };
        var open = options.Where(o => Realm(p).EquipSlots.Contains(o)).ToArray();
        if (open.Length == 0)
        {
            var opens = GameData.Realms.FirstOrDefault(r => r.EquipSlots.Contains(def.Slot));
            throw new GameException($"Ô {GameData.Slots.First(x => x.Id == def.Slot).Name} mở ở cảnh giới {opens?.Name}.");
        }
        string target;
        if (targetSlot is not null)
        {
            if (!open.Contains(targetSlot)) throw new GameException($"{def.Name} không mặc được vào ô này.");
            target = targetSlot;
        }
        else target = open.FirstOrDefault(o => p.Equipment.All(e => e.Slot != o)) ?? open[0];

        var slot = p.Equipment.FirstOrDefault(e => e.Slot == target);
        if (slot is null) p.Equipment.Add(slot = new EquippedItem { PlayerId = p.Id, Slot = target });
        else p.Add(slot.ItemId, 1); // trả món cũ về túi
        slot.ItemId = itemId;
        p.Add(itemId, -1);
        return ($"Đã trang bị {def.Name}.", "good");
    }

    /// <summary>Dùng vật phẩm tiêu hao (đan dược hồi linh khí).</summary>
    public (string msg, string kind) Use(Player p, string itemId)
    {
        if (!GameData.ItemById.TryGetValue(itemId, out var def) || def.UseQiSeconds <= 0)
            throw new GameException("Vật phẩm này không dùng trực tiếp được.");
        if (p.Count(itemId) < 1) throw new GameException("Không có vật phẩm này trong hành trang.");
        var gain = Rate(p) * def.UseQiSeconds;
        var before = p.Qi;
        p.Qi = Math.Min(QiReq(p), p.Qi + gain);
        p.Add(itemId, -1);
        return Log(p, p.Qi > before ? $"Dùng {def.Name}: +{Math.Floor(p.Qi - before):N0} tu vi." : $"Dùng {def.Name}: tu vi đã đầy bình cảnh.", "good");
    }

    /// <summary>Bỏ vật phẩm khỏi túi.</summary>
    public (string msg, string kind) Drop(Player p, string itemId, int n)
    {
        if (!GameData.ItemById.TryGetValue(itemId, out var def)) throw new GameException("Vật phẩm không tồn tại.");
        n = Math.Clamp(n, 1, p.Count(itemId));
        if (p.Count(itemId) < 1) throw new GameException("Không có vật phẩm này trong hành trang.");
        p.Add(itemId, -n);
        return ($"Đã bỏ {n} {def.Name}.", "");
    }

    /// <summary>Cấp hiển thị: đếm liên tục qua các tầng/kỳ của mọi cảnh giới.</summary>
    public static int Level(Player p) => GameData.Realms.Take(p.Realm).Sum(r => r.Stages.Length) + p.Stage + 1;
    public static int LevelOf(int realm) => GameData.Realms.Take(realm).Sum(r => r.Stages.Length) + 1;

    public (string msg, string kind) Unequip(Player p, string slotId)
    {
        var slot = p.Equipment.FirstOrDefault(e => e.Slot == slotId) ?? throw new GameException("Ô này đang trống.");
        if (BagUsed(p) >= BagCap(p)) throw new GameException("Hành trang đầy, không tháo được.");
        p.Add(slot.ItemId, 1);
        p.Equipment.Remove(slot);
        db.Equipment.Remove(slot);
        return ($"Đã tháo {GameData.ItemById[slot.ItemId].Name}.", "");
    }

    public (string msg, string kind) Advance(Player p)
    {
        var r = Realm(p);
        if (p.Qi < QiReq(p)) throw new GameException("Tu vi chưa đủ.");
        var cost = TargetCost(p);

        if (!IsPeak(p))
        {
            if (!p.Has(cost)) throw new GameException("Thiếu vật liệu.");
            p.Pay(cost);
            p.Stage++;
            p.Qi = 0;
            return Log(p, $"Đột phá thành công: {r.Name} {r.Stages[p.Stage]}.", "good");
        }

        var next = NextRealm(p) ?? throw new GameException($"Cảnh giới {GameData.LockedRealms[0]} chưa khai mở.");
        if (!p.Has(cost)) throw new GameException("Thiếu vật liệu.");
        p.Pay(cost);

        if (Random.Shared.NextDouble() < Chance(p))
        {
            p.Realm++;
            p.Stage = 0;
            p.Qi = 0;
            p.Bonus = 0;
            return Log(p, $"Thiên địa dị tượng! Đột phá thành công cảnh giới {next.Name}.", "gold");
        }

        p.Qi *= 0.6;
        p.Bonus = Math.Round(p.Bonus + 0.1, 2);
        return Log(p, $"Đột phá {next.Name} thất bại. Tổn 40% tu vi, lần sau tăng {p.Bonus * 100:0}% tỉ lệ.", "bad");
    }

    public (string msg, string kind) Craft(Player p, int index)
    {
        if (index < 0 || index >= GameData.Recipes.Count) throw new GameException("Công thức không tồn tại.");
        var rc = GameData.Recipes[index];
        if (p.Realm < rc.MinRealm) throw new GameException($"Cần cảnh giới {GameData.Realms[rc.MinRealm].Name}.");
        if (!p.Has(rc.In)) throw new GameException("Thiếu nguyên liệu.");

        p.Pay(rc.In);
        var name = GameData.ItemById[rc.OutItem].Name;
        if (Random.Shared.NextDouble() < rc.Rate)
        {
            p.Add(rc.OutItem, rc.OutCount);
            return Log(p, $"Luyện thành công {rc.OutCount} {name}.", "good");
        }
        return Log(p, $"Lò đan nổ, luyện {name} thất bại.", "bad");
    }

    public async Task<Player> ResetAsync(Player p)
    {
        db.Players.Remove(p);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        // giữ nguyên Id để client không phải đổi
        var fresh = await CreateAsync(p.Name, p.Id);
        AddLog(fresh, "Tán công trùng tu, bắt đầu lại từ Luyện Khí tầng 1.");
        await db.SaveChangesAsync();
        return fresh;
    }

    public async Task SaveAsync(Player p)
    {
        await db.SaveChangesAsync();
        // giới hạn nhật ký để DB không phình
        var cutoff = await db.Logs.Where(l => l.PlayerId == p.Id).OrderByDescending(l => l.Id)
            .Skip(LogKeep).Select(l => l.Id).FirstOrDefaultAsync();
        if (cutoff > 0) await db.Logs.Where(l => l.PlayerId == p.Id && l.Id <= cutoff).ExecuteDeleteAsync();
    }

    // ================= Nội bộ =================

    /// <summary>
    /// Cộng tu vi theo thời gian thực và chạy các chuyến thám hiểm tự động đã đến hạn
    /// (cả lúc người chơi vắng mặt, tối đa MaxOfflineSeconds).
    /// </summary>
    private void Tick(Player p, DateTime now)
    {
        var gap = Math.Clamp((now - p.LastTickAt).TotalSeconds, 0, GameData.MaxOfflineSeconds);
        var before = p.Qi;
        p.Qi = Math.Min(QiReq(p), p.Qi + Rate(p) * gap);
        p.LastTickAt = now;

        var trips = 0;
        var losses = 0;
        var lost = 0;   // vật phẩm bỏ lại vì túi đầy
        var loot = new Dictionary<string, int>();
        string? place = null;
        // luôn tự động thám hiểm (tu sĩ của bản cũ đang bế quan thì cho đi tiếp)
        if (!p.AutoExplore)
        {
            p.AutoExplore = true;
            p.ExploreReadyAt = now.AddSeconds(ExploreSeconds(p));
        }
        {
            var cd = ExploreSeconds(p);
            var t = p.ExploreReadyAt < now.AddSeconds(-GameData.MaxOfflineSeconds) ? now.AddSeconds(-GameData.MaxOfflineSeconds) : p.ExploreReadyAt;
            var luck = 1 + Stats(p).Sense / 100;
            var r = Realm(p);
            var battle = Battle(p);
            while (t <= now)
            {
                trips++;
                place = r.Places[Random.Shared.Next(r.Places.Length)];
                t = t.AddSeconds(cd);
                if (!battle.Win)
                {
                    // thua trận: rút lui, chỉ nhận nửa tu vi, không nhặt được gì
                    losses++;
                    p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) / 2);
                    continue;
                }
                foreach (var d in r.Drops)
                {
                    if (Random.Shared.NextDouble() >= Math.Min(1, d.Chance * luck)) continue;
                    var n = Random.Shared.Next(d.Min, d.Max + 1);
                    var got = TryAddLoot(p, d.Item, n);
                    if (got > 0) loot[d.Item] = loot.GetValueOrDefault(d.Item) + got;
                    lost += n - got;
                }
                p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p));
            }
            p.ExploreReadyAt = t;
        }

        var gained = Math.Floor(p.Qi - before);
        var qiText = p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy bình cảnh, cần đột phá" : $"+{gained:N0} tu vi";
        var lootText = string.Join(", ", loot.Select(kv => $"+{kv.Value} {GameData.ItemById[kv.Key].Name}"))
            + (lost > 0 ? $"{(loot.Count > 0 ? ", " : "")}hành trang đầy nên bỏ lại {lost} món" : "");
        var gotEquip = loot.Keys.Any(k => GameData.ItemById[k].Slot is not null);
        if (gap > 60)
            AddLog(p, trips > 0
                ? $"Vắng mặt {Math.Round(gap / 60)} phút: thám hiểm {trips} chuyến{(losses > 0 ? $" (thua {losses})" : "")}, {qiText}{(lootText.Length > 0 ? ", " + lootText : "")}."
                : $"Vắng mặt {Math.Round(gap / 60)} phút, thu được {gained:N0} tu vi.", "gold");
        else if (trips > 0 && losses == trips)
            AddLog(p, $"Thám hiểm {place}: bị {Realm(p).Monster.Name} đánh trọng thương, phải rút lui. Cần thêm tấn công, máu hoặc phòng thủ.", "bad");
        else if (trips > 0)
            AddLog(p, $"Thám hiểm {place}: hạ {Realm(p).Monster.Name}: {(p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy" : $"+{gained:N0} tu vi")}{(lootText.Length > 0 ? ", " + lootText : "")}.", gotEquip ? "gold" : "loot");
    }

    /// <summary>Tổng chỉ số % từ trang bị đang mặc.</summary>
    private static double Bonus(Player p, Func<ItemDef, double> stat) =>
        p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? stat(d) : 0);

    /// <summary>Tu vi mỗi giây đã tính trang bị.</summary>
    private static double Rate(Player p) =>
        Realm(p).Rate(p.Stage) * (1 + Bonus(p, i => i.QiPct) / 100);

    /// <summary>Thời lượng một chuyến: đi đường + số lượt đánh + kết thúc. Tấn công cao thì hạ quái nhanh hơn.</summary>
    private static double ExploreSeconds(Player p) =>
        Math.Max(GameData.MinExploreSeconds,
            GameData.WalkSeconds * (1 - Math.Min(60, Stats(p).Move) / 100)
            + Battle(p).Hits * GameData.HitSeconds * (1 - Math.Min(60, Stats(p).Speed) / 100)
            + GameData.EndSeconds);

    /// <summary>Chỉ số chiến đấu = chỉ số gốc theo cảnh giới/tầng + trang bị.</summary>
    public static CombatStats Stats(Player p)
    {
        var b = GameData.BaseStats(Realm(p), p.Stage);
        int Sum(Func<ItemDef, int> f) => p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? f(d) : 0);
        double SumD(Func<ItemDef, double> f) => p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? f(d) : 0);
        var ex = GameData.BaseExtra[Math.Min(p.Realm, GameData.BaseExtra.Length - 1)];
        bool On(string id) => p.Realm >= GameData.ExtraStats.First(x => x.Id == id).MinRealm;
        return new CombatStats(b.Atk + Sum(i => i.Atk), b.Hp + Sum(i => i.Hp), b.Def + Sum(i => i.Def),
            On("speed") ? ex.Speed + SumD(i => i.SpeedPct) : 0,
            On("sense") ? ex.Sense + SumD(i => i.LuckPct) : 0,
            On("spirit") ? ex.Spirit + SumD(i => i.SpiritPct) : 0,
            5 + SumD(i => i.CritPct),
            SumD(i => i.MovePct));
    }

    /// <summary>
    /// Trận đánh theo lượt, không ngẫu nhiên: tu sĩ chém trước, quái phản đòn sau mỗi lượt.
    /// Thắng nếu hạ được quái trước khi hết máu.
    /// </summary>
    public static BattleDto Battle(Player p)
    {
        var st = Stats(p);
        var m = Realm(p).Monster;
        var crit = Math.Min(100, st.Crit) / 100;
        var heroDmg = Math.Max(1, (int)Math.Round(st.Atk * (1 + st.Spirit / 100) * (1 + crit * 0.5)) - m.Def);
        var monDmg = Math.Max(1, m.Atk - st.Def);
        var toKill = (int)Math.Ceiling(m.Hp / (double)heroDmg);
        var toDie = (int)Math.Ceiling(st.Hp / (double)monDmg);   // số lượt phản đòn tu sĩ chịu được
        var win = toKill <= toDie;                                // phản đòn chỉ xảy ra khi quái còn sống
        var hits = Math.Min(GameData.MaxHits, win ? toKill : toDie);
        return new BattleDto(m.Name, m.Image, m.Hp, m.Atk, m.Def, heroDmg, monDmg, hits, win);
    }

    private static double ExploreQi(Player p) => Realm(p).Rate(p.Stage) * GameData.ExploreQiSeconds;

    private static RealmDef Realm(Player p) => GameData.Realms[p.Realm];
    private static double QiReq(Player p) => Realm(p).QiReq(p.Stage);
    private static bool IsPeak(Player p) => p.Stage == Realm(p).Stages.Length - 1;

    private static RealmDef? NextRealm(Player p) =>
        Realm(p).BreakCost is not null && p.Realm + 1 < GameData.Realms.Count ? GameData.Realms[p.Realm + 1] : null;

    private static Dictionary<string, int> TargetCost(Player p) =>
        IsPeak(p) ? Realm(p).BreakCost ?? new() : Realm(p).StageCost(p.Stage);

    private static double Chance(Player p) => Math.Min(0.95, Realm(p).BreakChance + p.Bonus);

    private void AddLog(Player p, string text, string kind = "")
    {
        var entry = new LogEntry { PlayerId = p.Id, Text = text, Kind = kind, At = DateTime.UtcNow };
        p.Logs.Add(entry);
        db.Logs.Add(entry);
    }

    private (string, string) Log(Player p, string text, string kind)
    {
        AddLog(p, text, kind);
        return (text, kind);
    }
}
