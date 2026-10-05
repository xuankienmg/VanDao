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
        AddLog(p, "Đạo tâm vừa khởi. Tu vi tự tăng khi ngồi thiền, bấm Vận công để tăng nhanh.");
        await db.SaveChangesAsync();
        return p;
    }

    /// <summary>Nạp người chơi và cộng tu vi theo thời gian thực đã trôi qua.</summary>
    public async Task<Player> LoadAsync(Guid id)
    {
        var p = await db.Players.Include(x => x.Inventory).FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException();
        Tick(p, DateTime.UtcNow);
        return p;
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
            p.Qi, r.QiReq(p.Stage), r.Rate(p.Stage), r.Rate(p.Stage) * GameData.ChannelMultiplier,
            peak, next is not null, title, kind,
            cost.Select(kv => new CostDto(kv.Key, kv.Value, p.Count(kv.Key))).ToList(),
            Chance(p), p.Bonus,
            Math.Max(0, (long)(p.ExploreReadyAt - DateTime.UtcNow).TotalMilliseconds),
            p.Inventory.Where(i => i.Count > 0).ToDictionary(i => i.ItemId, i => i.Count),
            GameData.Recipes.Select((rc, i) => new RecipeStateDto(i, p.Realm < rc.MinRealm, p.Realm >= rc.MinRealm && p.Has(rc.In))).ToList(),
            logs.Take(LogShow).ToList());
    }

    // ================= Hành động =================

    public string Channel(Player p)
    {
        var gain = Realm(p).Rate(p.Stage) * GameData.ChannelMultiplier;
        p.Qi = Math.Min(QiReq(p), p.Qi + gain);
        return $"+{gain:0.#} tu vi";
    }

    public (string msg, string kind) Explore(Player p)
    {
        var now = DateTime.UtcNow;
        if (now < p.ExploreReadyAt)
            throw new GameException($"Chưa hồi phục, còn {Math.Ceiling((p.ExploreReadyAt - now).TotalSeconds)} giây.");

        var r = Realm(p);
        var got = new List<string>();
        foreach (var d in r.Drops)
        {
            if (Random.Shared.NextDouble() >= d.Chance) continue;
            var n = Random.Shared.Next(d.Min, d.Max + 1);
            p.Add(d.Item, n);
            got.Add($"+{n} {GameData.ItemById[d.Item].Name}");
        }
        p.ExploreReadyAt = now.AddSeconds(r.ExploreCooldownSeconds);
        var place = r.Places[Random.Shared.Next(r.Places.Length)];
        var msg = $"Thám hiểm {place}: {(got.Count > 0 ? string.Join(", ", got) : "không thu hoạch được gì")}.";
        AddLog(p, msg);
        return (msg, "");
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

    private void Tick(Player p, DateTime now)
    {
        var gap = Math.Clamp((now - p.LastTickAt).TotalSeconds, 0, GameData.MaxOfflineSeconds);
        var before = p.Qi;
        p.Qi = Math.Min(QiReq(p), p.Qi + Realm(p).Rate(p.Stage) * gap);
        p.LastTickAt = now;
        if (gap > 60 && p.Qi > before)
            AddLog(p, $"Bế quan {Math.Round(gap / 60)} phút, thu được {Math.Floor(p.Qi - before):N0} tu vi.", "gold");
    }

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
