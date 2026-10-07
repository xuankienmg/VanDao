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
        ConvertLegacy(p);
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
        else { title = $"{r.Name} · {r.Stages[p.Stage + 1]}"; kind = "Đột phá tầng tiếp theo"; }

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
            MapState(p),
            SkillLevels(p), p.Slots(), SkillPoints(p), SkillData.PointsEarned(Level(p), p.Realm, p.BossKills),
            new BreakStateDto(Realm(p).BreakChance, Math.Max(0, (long)(p.BreakCooldownUntil - DateTime.UtcNow).TotalMilliseconds),
                Math.Max(0, (long)(p.DebuffUntil - DateTime.UtcNow).TotalMilliseconds), BreakPreview(p)),
            logs.Take(LogShow).ToList(), p.Admin);
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
            throw new GameException("Đan dược chỉ dùng khi đột phá.");
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

    /// <summary>Bán trang bị trong hành trang lấy linh thạch (món đang mặc không bán được).</summary>
    public (string msg, string kind) Sell(Player p, string itemId, int n)
    {
        if (!GameData.ItemById.TryGetValue(itemId, out var def) || def.Sell <= 0) throw new GameException("Chỉ bán được trang bị.");
        if (p.Count(itemId) < 1) throw new GameException("Không có món này trong hành trang.");
        n = Math.Clamp(n, 1, p.Count(itemId));
        p.Add(itemId, -n);
        p.Add("linh_thach", def.Sell * n);
        return Log(p, $"Bán {(n > 1 ? n + " " : "")}{def.Name}: +{def.Sell * n:N0} linh thạch.", "loot");
    }

    /// <summary>Bán hết trang bị trong hành trang có độ hiếm từ maxRarity trở xuống (0 = Phàm phẩm … 4 = Tiên phẩm).</summary>
    public (string msg, string kind) SellBulk(Player p, int maxRarity)
    {
        var list = p.Inventory.Where(i => i.Count > 0 && GameData.ItemById.TryGetValue(i.ItemId, out var d) && d.Sell > 0
                && Array.IndexOf(GameData.RarityOrder, d.Rarity) <= maxRarity).ToList();
        if (list.Count == 0) throw new GameException("Không có trang bị nào để bán.");
        int count = 0, gain = 0;
        foreach (var i in list)
        {
            var d = GameData.ItemById[i.ItemId];
            count += i.Count; gain += d.Sell * i.Count;
            p.Add(i.ItemId, -i.Count);
        }
        p.Add("linh_thach", gain);
        return Log(p, $"Bán {count} món trang bị: +{gain:N0} linh thạch.", "loot");
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
        if (!p.Has(cost)) throw new GameException(p.Count(r.BossItem) < 1 ? $"Thiếu {GameData.ItemById[r.BossItem].Name}: đánh bại Boss {r.Boss.Name} để nhận." : "Thiếu vật liệu.");
        var now = DateTime.UtcNow;
        if (p.Cheat("break")) { p.DebuffUntil = p.BreakCooldownUntil = DateTime.MinValue; }   // admin: bỏ điều tức
        if (now < p.DebuffUntil) throw new GameException($"Kinh mạch còn hỗn loạn sau tẩu hỏa nhập ma, cần điều tức thêm {Math.Ceiling((p.DebuffUntil - now).TotalSeconds)} giây.");
        if (now < p.BreakCooldownUntil) throw new GameException($"Khí tức chưa ổn định, chờ thêm {Math.Ceiling((p.BreakCooldownUntil - now).TotalSeconds)} giây.");
        var chance = Chance(p);
        var roll = Random.Shared.NextDouble();

        if (roll >= chance)
        {
            // thất bại: mất đan và linh thạch, yêu hạch Boss được giữ lại
            p.Pay(cost.Where(kv => kv.Key != r.BossItem).ToDictionary(kv => kv.Key, kv => kv.Value));
            var major = roll >= 1 - (1 - chance) * GameData.BreakMajorShare;
            var loss = major ? GameData.BreakMajorLoss : GameData.BreakFailLoss;
            p.Qi = Math.Max(0, p.Qi - QiReq(p) * loss);
            p.Bonus = Math.Round(p.Bonus + 0.1, 2);
            p.BreakCooldownUntil = now.AddSeconds(major ? GameData.BreakMajorCooldown : GameData.BreakFailCooldown);
            if (major) p.DebuffUntil = now.AddSeconds(GameData.DebuffSeconds);
            return Log(p, major
                ? $"Tẩu hỏa nhập ma! Đột phá {next.Name} thất bại, tổn {loss * 100:0}% tu vi, suy yếu {GameData.DebuffSeconds / 60:0} phút. Lần sau +{p.Bonus * 100:0}% tỉ lệ."
                : $"Linh khí phản phệ, đột phá {next.Name} thất bại. Tổn {loss * 100:0}% tu vi, lần sau +{p.Bonus * 100:0}% tỉ lệ.", major ? "major" : "bad");
        }

        p.Pay(cost);
        var crit = roll < chance * GameData.BreakCritShare;
        {
            p.Realm++;
            p.Stage = 0;
            p.Qi = 0;
            p.Bonus = 0;
            p.Area = 1;
            p.AreaMax = 1;
            p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
            if (crit)
            {
                var nb = GameData.BaseStats(next, 0);
                p.BodyHp += (int)Math.Round(nb.Hp * GameData.BreakCritBonus);
                p.BodyDef += (int)Math.Round(nb.Def * GameData.BreakCritBonus);
                return Log(p, $"Đại thành công! Đạo cơ vững chắc, bước vào {next.Name}: vĩnh viễn +{nb.Hp * GameData.BreakCritBonus:0} máu, +{nb.Def * GameData.BreakCritBonus:0} phòng thủ.", "crit");
            }
            return Log(p, $"Thiên địa dị tượng! Đột phá thành công cảnh giới {next.Name}.", "gold");
        }
    }

    /// <summary>Thay đổi khi đột phá kế tiếp: chỉ số gốc, tốc độ tu luyện, ô hành trang, ô trang bị và chỉ số phụ mới mở.</summary>
    private static BreakPreviewDto BreakPreview(Player p)
    {
        var r = Realm(p);
        var next = IsPeak(p) ? NextRealm(p) : r;
        if (next is null) return new BreakPreviewDto(0, 0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>());
        var ns = IsPeak(p) ? 0 : p.Stage + 1;
        var a = GameData.BaseStats(r, p.Stage); var b = GameData.BaseStats(next, ns);
        var rate = r.Rate(p.Stage); var rate2 = next.Rate(ns);
        var slots = next.EquipSlots.Except(r.EquipSlots).Select(s => GameData.Slots.First(x => x.Id == s).Name).ToArray();
        var stats = next == r ? Array.Empty<string>() : GameData.ExtraStats.Where(x => x.MinRealm == p.Realm + 1).Select(x => x.Name).ToArray();
        return new BreakPreviewDto(b.Hp - a.Hp, b.Atk - a.Atk, b.Def - a.Def, Math.Round((rate2 / rate - 1) * 100), next.BagSlots - r.BagSlots, slots, stats);
    }

    // ================= Admin mode (bật/tắt bằng Ctrl+Shift+A ở giao diện) =================

    public static readonly string[] AdminFlags = { "god", "break", "fast" };

    /// <summary>Lệnh admin: give · stones · qi · realm · clearcd · points · maxfire · resetskills · unlock · explore · flag.</summary>
    public (string msg, string kind) AdminAction(Player p, string action, string? arg, int n)
    {
        n = Math.Clamp(n, 1, 1_000_000_000);
        switch (action)
        {
            case "give":
                if (arg is null || !GameData.ItemById.TryGetValue(arg, out var it)) throw new GameException("Vật phẩm không tồn tại.");
                p.Add(arg, n);
                return Log(p, $"[Admin] +{n:N0} {it.Name}.", "good");
            case "stones":
                p.Add("linh_thach", n);
                return Log(p, $"[Admin] +{n:N0} linh thạch.", "good");
            case "qi":
                p.Qi = QiReq(p);
                return Log(p, "[Admin] Tu vi đầy bình cảnh.", "good");
            case "realm":
            {
                var parts = (arg ?? "").Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out var ri) || !int.TryParse(parts[1], out var si)
                    || ri < 0 || ri >= GameData.Realms.Count || si < 0 || si >= GameData.Realms[ri].Stages.Length)
                    throw new GameException("Cảnh giới / tầng không hợp lệ.");
                p.Realm = ri; p.Stage = si; p.Qi = 0; p.Bonus = 0; p.Area = 1; p.AreaMax = 1;
                p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
                foreach (var e in p.Equipment.Where(e => !Realm(p).EquipSlots.Contains(e.Slot)).ToList()) { p.Add(e.ItemId, 1); p.Equipment.Remove(e); db.Equipment.Remove(e); }
                return Log(p, $"[Admin] Chuyển tới {Realm(p).Name} {Realm(p).Stages[si]}.", "gold");
            }
            case "clearcd":
                p.BreakCooldownUntil = p.DebuffUntil = DateTime.MinValue;
                return Log(p, "[Admin] Đã xóa điều tức và suy yếu.", "good");
            case "points":
                p.AdminPoints += n;
                return Log(p, $"[Admin] +{n} điểm kỹ năng.", "good");
            case "maxfire":
            {
                var lv = SkillLevels(p);
                foreach (var s in SkillData.Skills.Where(s => s.Element == "hoa")) lv[s.Id] = s.MaxLv;
                p.SetSkillLevels(lv);
                if (SkillPoints(p) < 0) p.AdminPoints += -SkillPoints(p);
                return Log(p, "[Admin] Học tối đa toàn bộ cây Hỏa hệ.", "good");
            }
            case "resetskills":
                p.SetSkillLevels(new Dictionary<string, int> { [SkillData.StartSkill] = 1 });
                p.SkillSlots = "";
                p.AdminPoints = 0;                                   // về đúng số điểm kiếm được
                return Log(p, "[Admin] Đã reset cây kỹ năng, hoàn toàn bộ điểm.", "good");
            case "unlock":
                p.AreaMax = GameData.Areas;
                p.Add(Realm(p).KeyItem, 1);
                return Log(p, $"[Admin] Mở toàn bộ ải {p.Realm + 1}-1 → {p.Realm + 1}-{GameData.Areas}, +1 {GameData.ItemById[Realm(p).KeyItem].Name}.", "good");
            case "explore":
                p.ExploreReadyAt = DateTime.UtcNow;
                return Log(p, "[Admin] Chuyến thám hiểm kết thúc ngay.", "good");
            case "flag":
            {
                if (arg is null || !AdminFlags.Contains(arg)) throw new GameException("Cờ admin không hợp lệ.");
                var set = p.Admin.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var on = set.Add(arg); if (!on) set.Remove(arg);
                p.Admin = string.Join(',', set);
                if (arg == "fast") p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
                return Log(p, $"[Admin] {(arg == "god" ? "Bất tử" : arg == "break" ? "Đột phá luôn thành công" : "Thám hiểm nhanh")}: {(on ? "BẬT" : "TẮT")}.", on ? "good" : "");
            }
            default: throw new GameException("Lệnh admin không tồn tại.");
        }
    }

    // ================= Công pháp =================

    public static Dictionary<string, int> SkillLevels(Player p)
    {
        var d = p.SkillLevels();
        if (!d.ContainsKey(SkillData.StartSkill)) d[SkillData.StartSkill] = 1;   // linh căn bẩm sinh
        return d;
    }

    public static int SkillPointsSpent(Player p) =>
        SkillLevels(p).Sum(kv => SkillData.ById.TryGetValue(kv.Key, out var s) && s.Tree != "basic"   // đòn thường không dùng điểm
            ? Enumerable.Range(0, kv.Value).Sum(l => s.Id == SkillData.StartSkill && l == 0 ? 0 : s.CostAt(l)) : 0);

    public static int SkillPoints(Player p) => SkillData.PointsEarned(Level(p), p.Realm, p.BossKills) + p.AdminPoints - SkillPointsSpent(p);

    /// <summary>Nâng một kỹ năng lên 1 cấp: hệ đã mở, đủ cảnh giới, đủ kỹ năng tiền đề, đủ điểm.</summary>
    public (string msg, string kind) UpgradeSkill(Player p, string id)
    {
        if (!SkillData.ById.TryGetValue(id, out var s)) throw new GameException("Kỹ năng không tồn tại.");
        if (!SkillData.Elements.First(e => e.Id == s.Element).Open) throw new GameException("Hệ này chưa mở khóa.");
        var lv = SkillLevels(p);
        if (s.Tree == "basic") throw new GameException($"{s.Name} là đòn đánh thường, luôn sẵn có, không dùng điểm kỹ năng.");
        var cur = lv.GetValueOrDefault(id);
        if (cur >= s.MaxLv) throw new GameException($"{s.Name} đã đạt cấp tối đa.");
        if (p.Realm < s.MinRealm) throw new GameException($"Cần cảnh giới {GameData.Realms[s.MinRealm].Name}.");
        foreach (var r in s.Req)
        {
            var (rid, rlv) = (r.Split(':')[0], int.Parse(r.Split(':')[1]));
            if (lv.GetValueOrDefault(rid) < rlv) throw new GameException($"Cần {SkillData.ById[rid].Name} cấp {rlv}.");
        }
        var cost = s.CostAt(cur);
        if (SkillPoints(p) < cost) throw new GameException($"Cần {cost} điểm kỹ năng.");
        lv[id] = cur + 1;
        p.SetSkillLevels(lv);
        if (cur == 0)
        {
            // lĩnh ngộ chiêu mới: tự đặt vào ô trống phù hợp
            var slots = p.Slots();
            var free = Enumerable.Range(0, SkillData.SkillSlotCount).FirstOrDefault(i => slots[i] == "" && SkillData.FitsSlot(s, i), -1);
            if (free >= 0) { slots[free] = id; p.SkillSlots = string.Join(',', slots); }
        }
        return Log(p, cur == 0 ? $"Lĩnh ngộ công pháp {s.Name}." : $"{s.Name} tăng lên cấp {cur + 1}.", "good");
    }

    /// <summary>Đặt kỹ năng vào ô (skill rỗng = tháo). Kỹ năng đã nằm ở ô khác sẽ được đổi chỗ.</summary>
    public (string msg, string kind) SetSkillSlot(Player p, int slot, string? skill)
    {
        if (slot < 0 || slot >= SkillData.SkillSlotCount) throw new GameException("Ô kỹ năng không hợp lệ.");
        var slots = p.Slots();
        if (string.IsNullOrEmpty(skill))
        {
            var old = slots[slot]; slots[slot] = "";
            p.SkillSlots = string.Join(',', slots);
            return (old.Length > 0 && SkillData.ById.TryGetValue(old, out var od) ? $"Tháo {od.Name}." : "", "");
        }
        if (!SkillData.ById.TryGetValue(skill, out var s)) throw new GameException("Kỹ năng không tồn tại.");
        if (s.Tree != "basic" && SkillLevels(p).GetValueOrDefault(skill) < 1) throw new GameException($"Chưa lĩnh ngộ {s.Name}.");
        if (!SkillData.FitsSlot(s, slot))
            throw new GameException(s.Category is "Căn cơ" or "Bị động" ? $"{s.Name} là bị động, luôn có hiệu lực, không cần đặt vào ô."
                : slot == SkillData.BasicSlot ? "Ô đòn thường chỉ đặt Hỏa Cầu hoặc Hỏa Tiễn." : $"{s.Name} là đòn đánh thường, đặt ở ô Đòn thường.");
        var at = Array.IndexOf(slots, skill);
        if (at >= 0 && at != slot) slots[at] = SkillData.FitsSlot(SkillData.ById.GetValueOrDefault(slots[slot]) ?? s, at) ? slots[slot] : "";
        slots[slot] = skill;
        p.SkillSlots = string.Join(',', slots);
        return ($"Trang bị {s.Name} vào ô {(slot == SkillData.BasicSlot ? "Đòn thường" : "Kỹ năng")}.", "good");
    }

    /// <summary>Tổng hiệu quả bị động của các kỹ năng đã học (đơn vị %).</summary>
    private static (double atk, double def, double hp, double crit, double speed, double move, double qi, double monAtk) SkillBonus(Player p)
    {
        double a = 0, d = 0, h = 0, c = 0, sp = 0, mv = 0, q = 0, m = 0;
        foreach (var (id, l) in SkillLevels(p))
        {
            if (!SkillData.ById.TryGetValue(id, out var s) || s.Tree == "basic") continue;
            var f = s.Factor(l);
            a += s.AtkPct * f; d += s.DefPct * f; h += s.HpPct * f; c += s.Crit * f; sp += s.Speed * f; mv += s.Move * f; q += s.QiPct * f; m += s.MonAtkDown * f;
        }
        return (a, d, h, c, sp, mv, q, Math.Min(40, m));
    }

    /// <summary>Bị động cho chiêu thức: % sát thương chiêu hệ Hỏa và % xuyên thấu phòng thủ quái.</summary>
    private static (double skill, double pen) SkillExtra(Player p)
    {
        double k = 0, pe = 0;
        foreach (var (id, l) in SkillLevels(p))
            if (SkillData.ById.TryGetValue(id, out var s) && s.Tree != "basic") { k += s.SkillPct * s.Factor(l); pe += s.Pen * s.Factor(l); }
        return (k, Math.Min(80, pe));
    }

    // ================= Đan dược: hợp thành 9 → 1 =================

    /// <summary>Hợp thành n lần (0 = tối đa): mỗi lần 9 viên bậc dưới + phí linh thạch → 1 viên bậc trên. Luôn thành công.</summary>
    public (string msg, string kind) Synthesize(Player p, string fromId, int n)
    {
        var sd = GameData.Synths.FirstOrDefault(x => x.From == fromId) ?? throw new GameException("Đan này không hợp thành tiếp được.");
        var max = MaxSynth(p, sd);
        if (max < 1)
            throw new GameException(p.Count(sd.From) < sd.Count
                ? $"Cần {sd.Count} {GameData.ItemById[sd.From].Name}, còn thiếu {sd.Count - p.Count(sd.From)}."
                : $"Cần {sd.Stones:N0} linh thạch phí hợp thành.");
        n = n <= 0 ? max : Math.Min(n, max);
        if (p.Count(sd.To) == 0 && BagUsed(p) >= BagCap(p) && p.Count(sd.From) > sd.Count * n)
            throw new GameException("Hành trang đầy.");
        p.Add(sd.From, -sd.Count * n);
        p.Add("linh_thach", -sd.Stones * n);
        p.Add(sd.To, n);
        return Log(p, $"Hợp thành {sd.Count * n} {GameData.ItemById[sd.From].Name} → {n} {GameData.ItemById[sd.To].Name}.", "gold");
    }

    private static int MaxSynth(Player p, SynthDef sd) =>
        Math.Min(p.Count(sd.From) / sd.Count, sd.Stones > 0 ? p.Count("linh_thach") / sd.Stones : int.MaxValue);

    // ================= Bản đồ: 20 ải + ải Boss =================

    /// <summary>Chọn ải để tự động thám hiểm. Ải 21 (Boss) cần chìa khóa và đã mở ải 20.</summary>
    public (string msg, string kind) SelectArea(Player p, int area)
    {
        var r = Realm(p);
        if (area == GameData.BossArea)
        {
            if (p.AreaMax < GameData.Areas) throw new GameException($"Mở ải {p.Realm + 1}-{GameData.Areas} trước khi khiêu chiến Boss.");
            if (p.Count(r.KeyItem) < 1) throw new GameException($"Cần {GameData.ItemById[r.KeyItem].Name} để vào {r.BossLair}. Rơi ở ải {p.Realm + 1}-1 đến {p.Realm + 1}-{GameData.Areas}.");
        }
        else if (area < 1 || area > p.AreaMax) throw new GameException("Ải này chưa mở.");
        if (p.Area == area) return ("", "");
        p.Area = area;
        p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));   // bắt đầu chuyến mới ở ải vừa chọn
        return area == GameData.BossArea
            ? Log(p, $"Cầm {GameData.ItemById[r.KeyItem].Name} tiến vào {r.BossLair}, khiêu chiến {r.Boss.Name}!", "gold")
            : ($"Chuyển sang ải {p.Realm + 1}-{area} · {GameData.AreaPlace(r, area)}.", "");
    }

    private static MapDto MapState(Player p)
    {
        var r = Realm(p);
        var areas = Enumerable.Range(1, GameData.BossArea).Select(a =>
        {
            var b = BattleAt(p, a);
            return new AreaDto(a, a == GameData.BossArea ? r.Boss.Name : r.Monster.Name, GameData.AreaPlace(r, a),
                b.MonsterHp, b.MonsterAtk, b.MonsterDef, b.Win, b.Hits, a <= p.AreaMax || (a == GameData.BossArea && p.AreaMax >= GameData.Areas));
        }).ToList();
        return new MapDto(p.Area, p.AreaMax, r.KeyItem, r.BossItem, r.BossLair, GameData.KeyChance(1), GameData.KeyChance(GameData.Areas), areas,
            GameData.Synths.Select(sd => new SynthStateDto(sd.From, MaxSynth(p, sd))).ToList(), p.Count(r.BossItem) > 0);
    }

    /// <summary>Quy đổi vật phẩm của bản cũ (3 loại linh thạch, nguyên liệu luyện đan, đan phẩm chất…).</summary>
    private static void ConvertLegacy(Player p)
    {
        foreach (var inv in p.Inventory.Where(i => i.Count > 0 && !GameData.ItemById.ContainsKey(i.ItemId)).ToList())
        {
            var id = inv.ItemId;
            var q = id.LastIndexOf("_q", StringComparison.Ordinal);
            if (q > 0 && GameData.ItemById.ContainsKey(id[..q])) p.Add(id[..q], inv.Count);              // đan phẩm chất → đan gốc
            else if (q > 0 && GameData.Legacy.TryGetValue(id[..q], out var lq)) p.Add(lq.To, inv.Count * lq.Factor);
            else if (GameData.Legacy.TryGetValue(id, out var l)) p.Add(l.To, inv.Count * l.Factor);
            inv.Count = 0;
        }
        p.RefineRecipe = -1;
        p.Area = Math.Clamp(p.Area, 1, GameData.BossArea);
        p.AreaMax = Math.Clamp(p.AreaMax, 1, GameData.Areas);
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
        string? special = null, specialKind = "gold";
        var startArea = p.Area;
        {
            var r = Realm(p);
            var luck = 1 + Stats(p).Sense / 100;
            var t = p.ExploreReadyAt < now.AddSeconds(-GameData.MaxOfflineSeconds) ? now.AddSeconds(-GameData.MaxOfflineSeconds) : p.ExploreReadyAt;
            while (t <= now)
            {
                var area = p.Area;
                var battle = BattleAt(p, area);
                trips++;
                place = GameData.AreaPlace(r, area);
                if (area == GameData.BossArea)
                {
                    // ải Boss: thắng thì mất chìa khóa và nhận yêu hạch; thua thì giữ chìa khóa
                    if (battle.Win && p.Count(r.KeyItem) > 0)
                    {
                        p.Add(r.KeyItem, -1);
                        p.Add(r.BossItem, 1);
                        p.BossKills++;
                        p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) * 5);
                        special = $"Đánh bại {r.Boss.Name} ở {r.BossLair}! Nhận {GameData.ItemById[r.BossItem].Name}, tiêu hao {GameData.ItemById[r.KeyItem].Name}.";
                    }
                    else
                    {
                        losses++;
                        special = $"Không địch nổi {r.Boss.Name}, rút khỏi {r.BossLair} (chìa khóa vẫn còn). Tu luyện thêm rồi quay lại.";
                        specialKind = "bad";
                    }
                    p.Area = GameData.Areas;
                    t = t.AddSeconds(ExploreSeconds(p));
                    continue;
                }
                t = t.AddSeconds(ExploreSeconds(p));
                if (!battle.Win)
                {
                    // thua trận: rút lui về ải trước, chỉ nhận nửa tu vi
                    losses++;
                    p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) / 2);
                    if (p.Area > 1) p.Area--;
                    continue;
                }
                var scale = 1 + (area - 1) * 0.05;
                foreach (var d in r.Drops)
                {
                    var isStone = d.Item == "linh_thach";
                    if (Random.Shared.NextDouble() >= Math.Min(1, d.Chance * luck * (isStone || d.Chance >= 1 ? 1 : 1 + (area - 1) * 0.03))) continue;
                    var n = (int)Math.Round(Random.Shared.Next(d.Min, d.Max + 1) * (isStone ? scale : 1));
                    var got = TryAddLoot(p, d.Item, n);
                    if (got > 0) loot[d.Item] = loot.GetValueOrDefault(d.Item) + got;
                    lost += n - got;
                }
                if (Random.Shared.NextDouble() < GameData.KeyChance(area) * luck && TryAddLoot(p, r.KeyItem, 1) > 0)
                    loot[r.KeyItem] = loot.GetValueOrDefault(r.KeyItem) + 1;
                p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p));
                // thắng ở ải cao nhất → mở và tiến sang ải kế
                if (area == p.AreaMax && p.AreaMax < GameData.Areas) { p.AreaMax++; p.Area = p.AreaMax; }
            }
            p.ExploreReadyAt = t;
        }

        var gained = Math.Floor(p.Qi - before);
        var qiText = p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy bình cảnh, cần đột phá" : $"+{gained:N0} tu vi";
        var lootText = string.Join(", ", loot.Select(kv => $"+{kv.Value} {GameData.ItemById[kv.Key].Name}"))
            + (lost > 0 ? $"{(loot.Count > 0 ? ", " : "")}hành trang đầy nên bỏ lại {lost} món" : "");
        var gotEquip = loot.Keys.Any(k => GameData.ItemById[k].Slot is not null || GameData.ItemById[k].Type == "key");
        if (special is not null) AddLog(p, special, specialKind);
        var areaTag = $"ải {p.Realm + 1}-{startArea}";
        if (p.Area != startArea && startArea <= GameData.Areas && trips > 0 && gap <= 60)
            AddLog(p, p.Area > startArea ? $"Qua {areaTag}, mở ải {p.Realm + 1}-{p.Area} · {GameData.AreaPlace(Realm(p), p.Area)}." : $"Thua ở {areaTag}, lùi về ải {p.Realm + 1}-{p.Area}.", p.Area > startArea ? "good" : "bad");
        if (gap > 60)
            AddLog(p, trips > 0
                ? $"Vắng mặt {Math.Round(gap / 60)} phút: thám hiểm {trips} chuyến{(losses > 0 ? $" (thua {losses})" : "")}, {qiText}{(lootText.Length > 0 ? ", " + lootText : "")}."
                : $"Vắng mặt {Math.Round(gap / 60)} phút, thu được {gained:N0} tu vi.", "gold");
        else if (special is not null && trips == 1) { }
        else if (trips > 0 && losses == trips)
            AddLog(p, $"Thám hiểm {areaTag} · {place}: bị {Realm(p).Monster.Name} đánh trọng thương, phải rút lui. Cần thêm tấn công, máu hoặc phòng thủ.", "bad");
        else if (trips > 0)
            AddLog(p, $"Thám hiểm {areaTag} · {place}: hạ {Realm(p).Monster.Name}: {(p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy" : $"+{gained:N0} tu vi")}{(lootText.Length > 0 ? ", " + lootText : "")}.", gotEquip ? "gold" : "loot");
    }

    /// <summary>Tổng chỉ số % từ trang bị đang mặc.</summary>
    private static double Bonus(Player p, Func<ItemDef, double> stat) =>
        p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? stat(d) : 0);

    /// <summary>Tu vi mỗi giây đã tính trang bị.</summary>
    private static double Rate(Player p) =>
        Realm(p).Rate(p.Stage) * (1 + (Bonus(p, i => i.QiPct) + SkillBonus(p).qi) / 100) * (DateTime.UtcNow < p.DebuffUntil ? GameData.DebuffRate : 1);

    /// <summary>Thời lượng một chuyến: đi đường + số lượt đánh + kết thúc. Tấn công cao thì hạ quái nhanh hơn.</summary>
    private static double ExploreSeconds(Player p) => p.Cheat("fast") ? 3 :   // admin: chuyến thám hiểm 3 giây
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
        var weak = DateTime.UtcNow < p.DebuffUntil ? GameData.DebuffStat : 1;   // tẩu hỏa nhập ma
        var sk = SkillBonus(p);                                                  // công pháp bị động
        return new CombatStats((int)((b.Atk + Sum(i => i.Atk)) * weak * (1 + sk.atk / 100)), (int)((b.Hp + Sum(i => i.Hp) + p.BodyHp) * (1 + sk.hp / 100)),
            (int)((b.Def + Sum(i => i.Def) + p.BodyDef) * weak * (1 + sk.def / 100)),
            (On("speed") ? ex.Speed + SumD(i => i.SpeedPct) : 0) + sk.speed,
            On("sense") ? ex.Sense + SumD(i => i.LuckPct) : 0,
            On("spirit") ? ex.Spirit + SumD(i => i.SpiritPct) : 0,
            5 + SumD(i => i.CritPct) + sk.crit,
            SumD(i => i.MovePct) + sk.move);
    }

    /// <summary>
    /// Trận đánh theo lượt, không ngẫu nhiên: tu sĩ chém trước, quái phản đòn sau mỗi lượt.
    /// Thắng nếu hạ được quái trước khi hết máu.
    /// </summary>
    public static BattleDto Battle(Player p) => BattleAt(p, p.Area);

    public static BattleDto BattleAt(Player p, int area)
    {
        var st = Stats(p);
        var m = GameData.AreaMonster(Realm(p), area);
        var down = SkillBonus(p).monAtk;                       // công pháp khống chế làm yếu đòn quái
        if (down > 0) m = m with { Atk = (int)Math.Round(m.Atk * (1 - down / 100)) };
        var (skillPct, pen) = SkillExtra(p);                   // Hỏa Tụ / Liệt Diễm Tâm / Xích Diễm Đạo Thể · Hỏa Linh Chân Ý
        if (pen > 0) m = m with { Def = (int)Math.Round(m.Def * (1 - pen / 100)) };
        var crit = Math.Min(100, st.Crit) / 100;
        var power = st.Atk * (1 + st.Spirit / 100) * (1 + crit * 0.5);
        // đòn đánh thường = chiêu ở ô 1 (Hỏa Cầu / Hỏa Tiễn), chưa đặt thì đánh 100% công
        var lv = SkillLevels(p);
        var basicId = p.Slots()[SkillData.BasicSlot];
        var basicOk = SkillData.ById.TryGetValue(basicId, out var bdef) && bdef.Tree == "basic";
        if (!basicOk) { basicId = "hoa_cau"; bdef = SkillData.ById[basicId]; }        // mặc định: Hỏa Cầu
        var bcrit = Math.Min(100, st.Crit + bdef!.Crit) / 100;                       // Hỏa Tiễn: bộc phát, dễ chí mạng
        var bpower = st.Atk * (1 + st.Spirit / 100) * (1 + bcrit * 0.5);
        var heroDmg = Math.Max(1, (int)Math.Round(bpower * bdef.DmgBase * (1 + skillPct / 100) / 100) - m.Def);

        // kỹ năng trong ô: chiêu công tự thi triển theo lượt, chiêu thủ giảm sát thương, khống làm quái đứng yên, thân pháp né đòn
        var equipped = p.Slots().Skip(1).Where(id => id.Length > 0 && lv.GetValueOrDefault(id) > 0 && SkillData.ById.ContainsKey(id))
            .Select(id => (s: SkillData.ById[id], l: lv[id])).ToList();
        double shield = 0, dodge = 0, burn = 0; int stun = 0;
        foreach (var (s, l) in equipped)
        {
            var a = SkillData.Active(s, l);
            shield += a.shield; dodge += a.dodge; stun += a.stun;
            burn += a.burn > 0 ? SkillData.Damage(s, l) * a.burn / 100 : 0;
        }
        shield = Math.Min(60, shield); dodge = Math.Min(50, dodge); stun = Math.Min(4, stun);
        var monDmg = p.Cheat("god") ? 0 : Math.Max(1, (int)Math.Round(Math.Max(1, m.Atk - st.Def) * (1 - shield / 100) * (1 - dodge / 100)));   // admin: bất tử

        int monHp = m.Hp, heroHp = st.Hp, turns = 0; var win = false;
        var monLeft = new List<int>(); var heroLeft = new List<int>(); var skip = new List<bool>(); var casts = new List<CastDto>();
        for (var t = 0; t < 60; t++)
        {
            var dmg = heroDmg;
            if (burn > 0 && t > 0) dmg += Math.Max(1, (int)Math.Round(power * burn / 100) - m.Def / 2);
            foreach (var (s, l) in equipped)
            {
                var a = SkillData.Active(s, l);
                var pct = SkillData.Damage(s, l);
                if (pct == 0 || a.stun > 0 && a.burn > 0)
                {
                    if (t == 0) casts.Add(new CastDto(0, s.Id, s.Name, s.Anim, 0));   // chiêu thủ / khống / thân pháp: thi triển đầu trận
                    continue;
                }
                var fire = a.every == 0 ? t == a.first : t >= a.first && (t - a.first) % a.every == 0;
                if (!fire) continue;
                var cd = Math.Max(1, (int)Math.Round(power * pct * (1 + skillPct / 100) / 100) - m.Def);
                dmg += cd;
                casts.Add(new CastDto(t, s.Id, s.Name, s.Anim, cd));
            }
            monHp -= dmg; turns = t + 1;
            monLeft.Add(Math.Max(0, monHp));
            if (monHp <= 0) { win = true; heroLeft.Add(heroHp); skip.Add(true); break; }
            var stunned = t < stun;
            if (!stunned) heroHp -= monDmg;
            heroLeft.Add(Math.Max(0, heroHp)); skip.Add(stunned);
            if (heroHp <= 0) break;
        }
        // hoạt ảnh tối đa MaxHits lượt: trận dài hơn thì lấy mẫu đều các lượt
        var hits = Math.Min(GameData.MaxHits, turns);
        int Pick(int i) => turns <= hits ? i : (int)Math.Round((i + 1) * (double)turns / hits) - 1;
        var idx = Enumerable.Range(0, hits).Select(Pick).ToArray();
        var castShown = casts.Where(c => idx.Contains(c.Turn)).Select(c => c with { Turn = Array.IndexOf(idx, c.Turn) }).ToList();
        return new BattleDto(m.Name, m.Image, m.Hp, m.Atk, m.Def, heroDmg, monDmg, hits, win,
            idx.Select(i => monLeft[i]).ToArray(), idx.Select(i => heroLeft[i]).ToArray(), idx.Select(i => skip[i]).ToArray(), castShown, st.Hp, basicId);
    }

    private static double ExploreQi(Player p) => Realm(p).Rate(p.Stage) * GameData.ExploreQiSeconds;

    private static RealmDef Realm(Player p) => GameData.Realms[p.Realm];
    private static double QiReq(Player p) => Realm(p).QiReq(p.Stage);
    private static bool IsPeak(Player p) => p.Stage == Realm(p).Stages.Length - 1;

    private static RealmDef? NextRealm(Player p) =>
        Realm(p).BreakCost is not null && p.Realm + 1 < GameData.Realms.Count ? GameData.Realms[p.Realm + 1] : null;

    private static Dictionary<string, int> TargetCost(Player p) =>
        IsPeak(p) ? Realm(p).BreakCost ?? new() : Realm(p).StageCost(p.Stage);

    private static double Chance(Player p) => p.Cheat("break") || Realm(p).BreakChance >= 1 ? 1 : Math.Clamp(Realm(p).BreakChance + p.Bonus, 0, 0.95);

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
