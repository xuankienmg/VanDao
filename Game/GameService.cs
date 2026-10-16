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
        // trang bị bản cũ (chưa có phẩm / sai cấp) đang mặc → món đúng cấp cùng phẩm, mặc nguyên tại chỗ
        foreach (var e in p.Equipment) if (GameData.UpgradeGear(e.ItemId) is { } up) e.ItemId = up;
        // món đang mặc đã bị bỏ khỏi game → tháo vào túi để ConvertLegacy quy đổi ra linh thạch
        foreach (var e in p.Equipment.Where(e => !GameData.ItemById.ContainsKey(e.ItemId)).ToList())
        {
            p.Add(e.ItemId, 1);
            p.Equipment.Remove(e);
            db.Equipment.Remove(e);
        }
        ConvertLegacy(p);
        // đòn thường đã bỏ (Hỏa Tiễn, Thủy Châm) → đổi sang đòn thường còn lại cùng hệ
        if (p.SkillSlots.Length > 0)
            p.SkillSlots = string.Join(',', p.SkillSlots.Split(',').Select(x => SkillData.RemovedBasics.TryGetValue(x.Trim(), out var to) ? to : x));
        // luật điểm mới (tầng/kỳ +1, đại cảnh giới +12, hạ Boss không cho điểm): đã dùng quá số điểm → tẩy kỹ năng, hoàn điểm
        if (SkillPoints(p) < 0)
        {
            p.SetSkillLevels(new Dictionary<string, int> { [SkillData.StartSkill] = 1 });
            p.SkillSlots = "";
            Log(p, $"Luật điểm kỹ năng đã đổi (+{SkillData.PointsPerStage} mỗi tầng/kỳ, +{SkillData.PointsPerRealm} mỗi đại cảnh giới): kỹ năng đã được tẩy, hoàn lại {SkillPoints(p)} điểm để lĩnh ngộ lại.", "gold");
        }
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
            new BonusDto(Bonus(p, i => i.QiPct), Bonus(p, i => i.SpeedPct)),
            Stats(p),
            Battle(p),
            r.Cn, next?.Name ?? GameData.LockedRealms[0], next?.Cn ?? GameData.LockedRealmsCn[0], next is null && peak,
            BagUsed(p), BagCap(p), r.EquipSlots, Level(p),
            p.Inventory.Where(i => i.Count > 0).ToDictionary(i => i.ItemId, i => i.Count),
            MapState(p),
            SkillLevels(p), p.Slots(), SkillPoints(p), SkillData.PointsEarned(Level(p), p.Realm, p.BossKills),
            new BreakStateDto(Realm(p).BreakChance, Math.Max(0, (long)(p.BreakCooldownUntil - DateTime.UtcNow).TotalMilliseconds),
                Math.Max(0, (long)(p.DebuffUntil - DateTime.UtcNow).TotalMilliseconds), BreakPreview(p)),
            logs.Take(LogShow).ToList(), p.Admin, ChestOdds(p));
    }

    /// <summary>Tỉ lệ phần thưởng khi mở từng loại rương, tính đúng như OpenOne (dùng cho chế độ admin).</summary>
    public static List<ChestOddsDto> ChestOdds(Player p)
    {
        var r = Realm(p);
        var treasures = GameData.GearBases.Where(b => b.Slot == "offhand" && b.MinRealm <= Math.Max(1, p.Realm)).ToList();
        var gear = r.Drops.Where(dd => GameData.GearBaseById.TryGetValue(dd.Item, out var b) && b.Slot != "offhand").ToList();
        var gw = gear.Sum(g => g.Chance);
        var pill = r.StageCost(0).Keys.First(k => k != "linh_thach");
        return GameData.Chests.Select(kv =>
        {
            var cd = kv.Value;
            var pt = treasures.Count > 0 ? cd.Treasure : 0;
            var pg = gear.Count > 0 ? (1 - pt) * cd.GearShare : 0;
            var cq = cd.QualityAt(p.Realm); var qs = cq.Sum();
            return new ChestOddsDto(kv.Key, GameData.GearLevels[Math.Clamp(p.Realm, 0, GameData.GearLevels.Length - 1)],
                cq.Select(q => Math.Round(q / qs * 100, 2)).ToArray(),
                treasures.Select(t => new ItemQtyPctDto(t.Id, Math.Round(pt / treasures.Count * 100, 2))).ToList(),
                gear.Select(g => new ItemQtyPctDto(g.Item, Math.Round(pg * g.Chance / gw * 100, 2))).ToList(),
                pill, cd.PillMin, cd.PillMax, Math.Round((1 - pt - pg) * 100, 2));
        }).ToList();
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

    /// <summary>Dùng vật phẩm: rương thì mở (kèm kết quả cho thông báo), còn lại như Use.</summary>
    /// <param name="n">Số rương mở một lần (Ctrl + chuột phải: 0 = mở hết); vật phẩm khác luôn dùng 1.</param>
    public (string msg, string kind, CraftResultDto? craft) UseItem(Player p, string itemId, int n = 1)
    {
        if (GameData.ItemById.TryGetValue(itemId, out var d) && d.Type == "chest") return OpenChests(p, d, n);
        var (m, k) = Use(p, itemId);
        return (m, k, null);
    }

    /// <summary>
    /// Mở rương: 1 phần thưởng. Pháp bảo (ô offhand) với tỉ lệ nhỏ; còn lại chia giữa trang bị
    /// trong bảng rơi của cảnh giới hiện tại và đan dược dùng để lên tầng/kỳ của cảnh giới đó.
    /// Rương hiếm: pháp bảo nhiều hơn, trang bị chỉ lấy phẩm cao nhất, đan nhiều hơn.
    /// </summary>
    /// <summary>Mở n rương cùng loại (0 = mở hết), dừng khi hết rương hoặc hành trang đầy; gộp phần thưởng vào một thông báo.</summary>
    private (string msg, string kind, CraftResultDto craft) OpenChests(Player p, ItemDef chest, int n)
    {
        if (p.Count(chest.Id) < 1) throw new GameException("Không có rương này trong hành trang.");
        var want = n <= 0 ? p.Count(chest.Id) : Math.Min(n, p.Count(chest.Id));
        var got = new List<ItemQtyDto>();
        var opened = 0; var full = false;
        while (opened < want)
        {
            var res = OpenOne(p, chest);
            if (res is null) { full = true; break; }
            opened++;
            var i = got.FindIndex(g => g.Item == res.Item);
            if (i >= 0) got[i] = got[i] with { Qty = got[i].Qty + res.Qty }; else got.Add(res);
        }
        if (opened == 0) throw new GameException("Hành trang đầy, dọn bớt rồi mở rương.");
        // món giá trị nhất lên đầu (pháp bảo → phẩm cao → đan)
        int Rank(ItemQtyDto x) { var d = GameData.ItemById[x.Item]; return (d.Slot == "offhand" ? 10 : 0) + Array.IndexOf(GameData.RarityOrder, d.Rarity); }
        got = got.OrderByDescending(Rank).ToList();
        var best = GameData.ItemById[got[0].Item];
        var text = string.Join(", ", got.Select(g => $"{g.Qty} {GameData.Label(GameData.ItemById[g.Item])}{(GameData.ItemById[g.Item].Slot == "offhand" ? " (pháp bảo)" : "")}"));
        var (msg, kind) = Log(p, $"Mở {(opened > 1 ? $"{opened} " : "")}{chest.Name}: nhận {text}.{(full ? " Hành trang đầy, dừng lại." : "")}",
            best.Slot == "offhand" || Array.IndexOf(GameData.RarityOrder, best.Rarity) >= 2 ? "gold" : "loot");
        return (msg, kind, new CraftResultDto("chest:" + chest.Id, got[0], new() { new(chest.Id, opened) }, got));
    }

    /// <summary>Mở 1 rương; null = hành trang đầy (rương được giữ lại).</summary>
    private ItemQtyDto? OpenOne(Player p, ItemDef chest)
    {
        var cd = GameData.Chests[chest.Id];
        var r = Realm(p);
        var rng = Random.Shared;
        string item; int n = 1;
        var treasures = GameData.GearBases.Where(b => b.Slot == "offhand" && b.MinRealm <= Math.Max(1, p.Realm)).ToList();
        var gear = r.Drops.Where(dd => GameData.GearBaseById.TryGetValue(dd.Item, out var b) && b.Slot != "offhand").ToList();
        if (rng.NextDouble() < cd.Treasure && treasures.Count > 0)
            item = GameData.RollGear(treasures[rng.Next(treasures.Count)].Id, cd.QualityAt(p.Realm));
        else if (gear.Count > 0 && rng.NextDouble() < cd.GearShare)
        {
            var x = rng.NextDouble() * gear.Sum(g => g.Chance);     // chọn món theo trọng số = tỉ lệ rơi gốc, rồi gieo phẩm
            var pick = gear[^1].Item;
            foreach (var g in gear) { if ((x -= g.Chance) < 0) { pick = g.Item; break; } }
            item = GameData.RollGear(pick, cd.QualityAt(p.Realm));
        }
        else
        {
            item = r.StageCost(0).Keys.First(k => k != "linh_thach");     // đan lên tầng/kỳ của cảnh giới hiện tại
            n = rng.Next(cd.PillMin, cd.PillMax + 1);
        }
        p.Add(chest.Id, -1);
        var added = TryAddLoot(p, item, n);
        if (added < n)
        {
            if (added > 0) p.Add(item, -added);
            p.Add(chest.Id, 1);
            return null;
        }
        return new ItemQtyDto(item, n);
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

    /// <summary>Bán hết trang bị trong hành trang có độ hiếm từ maxRarity trở xuống (0 = Phàm cấp … 4 = Thiên cấp).</summary>
    /// <summary>Bán hàng loạt theo độ hiếm (≤ maxRarity). gearOnly: chỉ trang bị.
    /// items: giới hạn trong các món "id:số,id:số" (vd. trang hành trang đang xem); null = cả hành trang.
    /// Đồ đang mặc không nằm trong hành trang nên không bao giờ bị bán.</summary>
    /// pick: các món người chơi tự chọn (chọn nhiều trong hành trang).
    public (string msg, string kind) SellBulk(Player p, int maxRarity, bool gearOnly = false, string? items = null, bool pick = false)
    {
        Dictionary<string, int>? scope = null;
        if (!string.IsNullOrWhiteSpace(items))
        {
            scope = new();
            foreach (var part in items.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out var c) && c > 0) scope[kv[0]] = scope.GetValueOrDefault(kv[0]) + c;
            }
        }
        var list = p.Inventory.Where(i => i.Count > 0 && GameData.ItemById.TryGetValue(i.ItemId, out var d) && d.Sell > 0
                && (!gearOnly || d.Slot is not null)
                && (scope is null || scope.ContainsKey(i.ItemId))
                && Array.IndexOf(GameData.RarityOrder, d.Rarity) <= maxRarity).ToList();
        int count = 0, gain = 0;
        foreach (var i in list)
        {
            var d = GameData.ItemById[i.ItemId];
            var n = scope is null ? i.Count : Math.Min(i.Count, scope[i.ItemId]);
            count += n; gain += d.Sell * n;
            p.Add(i.ItemId, -n);
        }
        if (count == 0) throw new GameException(gearOnly ? "Không có trang bị nào để bán." : "Không có món nào để bán.");
        p.Add("linh_thach", gain);
        return Log(p, $"Bán {count} {(gearOnly ? "món trang bị" : "món")}{(scope is null ? "" : pick ? " đã chọn" : " (trang đang xem)")}: +{gain:N0} linh thạch.", "loot");
    }

    /// <summary>Cấp hiển thị: đếm liên tục qua các tầng/kỳ của mọi cảnh giới.</summary>
    public static int Level(Player p) => GameData.Realms.Take(p.Realm).Sum(r => r.Stages.Length) + p.Stage + 1;

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
            return Log(p, $"Đột phá thành công: {r.Name} {r.Stages[p.Stage]}, +{SkillData.PointsForBreak(false)} điểm kỹ năng.", "good");
        }

        var next = NextRealm(p) ?? throw new GameException($"Cảnh giới {GameData.LockedRealms[0]} chưa khai mở.");
        if (!p.Has(cost)) throw new GameException(p.Count(r.BossItem) < 1 ? $"Thiếu {GameData.ItemById[r.BossItem].Name}: đánh bại Boss {r.Boss.Name} (chương {(GameData.ChapterDropping(r.BossItem)?.Index ?? p.Realm) + 1}) để nhận." : "Thiếu vật liệu.");
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
            p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
            if (crit)
            {
                var nb = GameData.BaseStats(next, 0);
                p.BodyHp += (int)Math.Round(nb.Hp * GameData.BreakCritBonus);
                p.BodyDef += (int)Math.Round(nb.Def * GameData.BreakCritBonus);
                return Log(p, $"Đại thành công! Đạo cơ vững chắc, bước vào {next.Name}: vĩnh viễn +{nb.Hp * GameData.BreakCritBonus:0} máu, +{nb.Def * GameData.BreakCritBonus:0} phòng thủ, +{SkillData.PointsForBreak(true)} điểm kỹ năng.", "crit");
            }
            return Log(p, $"Thiên địa dị tượng! Đột phá thành công cảnh giới {next.Name}, +{SkillData.PointsForBreak(true)} điểm kỹ năng.", "gold");
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
        var rate = GameData.QiRate(r, p.Stage); var rate2 = GameData.QiRate(next, ns);
        var slots = next.EquipSlots.Except(r.EquipSlots).Select(s => GameData.Slots.First(x => x.Id == s).Name).ToArray();
        var stats = next == r ? Array.Empty<string>() : GameData.ExtraStats.Where(x => x.MinRealm == p.Realm + 1).Select(x => x.Name).ToArray();
        return new BreakPreviewDto(b.Hp - a.Hp, b.Atk - a.Atk, b.Def - a.Def, Math.Round((rate2 / rate - 1) * 100), next.BagSlots - r.BagSlots, slots, stats,
            SkillData.PointsForBreak(next != r));
    }

    // ================= Admin mode (bật/tắt bằng Ctrl+Shift+A ở giao diện) =================

    public static readonly string[] AdminFlags = { "god", "break", "fast", "fast2", "fast3", "fast5", "boss" };
    /// <summary>Admin thám hiểm nhanh: hệ số tăng tốc chuyến thám hiểm (×2 · ×3 · ×5; cờ cũ "fast" = ×3), 1 = tắt.</summary>
    private static double FastMult(Player p) => p.Cheat("fast5") ? 5 : p.Cheat("fast3") || p.Cheat("fast") ? 3 : p.Cheat("fast2") ? 2 : 1;

    /// <summary>Lệnh admin: give · stones · qi · realm · clearcd · points · maxfire · resetskills · resetall · unlock · explore · flag.</summary>
    public (string msg, string kind) AdminAction(Player p, string action, string? arg, int n)
    {
        n = Math.Clamp(n, 1, 1_000_000_000);
        switch (action)
        {
            case "give":
                if (arg is null || !GameData.ItemById.TryGetValue(arg, out var it)) throw new GameException("Vật phẩm không tồn tại.");
                p.Add(arg, n);
                return Log(p, $"[Admin] +{n:N0} {GameData.Label(it)}.", "good");
            case "giveset":
            {
                // arg = "<tên bộ>|<phẩm 0–3>": nhận n món mỗi loại của cả bộ (kể cả pháp bảo trong bộ) ở phẩm đã chọn
                var parts = (arg ?? "").Split('|');
                if (parts.Length != 2 || !int.TryParse(parts[1], out var q) || q < 0 || q >= GameData.QualityMult.Length) throw new GameException("Bộ / phẩm không hợp lệ.");
                var bases = GameData.GearBases.Where(b => b.Set == parts[0]).ToList();
                if (bases.Count == 0) throw new GameException("Bộ trang bị không tồn tại.");
                foreach (var b in bases) p.Add(GameData.GearId(b, q), n);
                return Log(p, $"[Admin] +{n:N0} bộ {parts[0]} [cấp {GameData.GearLevelOf(bases[0])} · {GameData.QualityName[q]}] ({bases.Count} món).", "good");
            }
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
                p.Realm = ri; p.Stage = si; p.Qi = 0; p.Bonus = 0;
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
            case "resetall":
            {
                // về đúng trạng thái nhân vật mới tạo: giữ Id, tên, ngày tạo và cờ admin mode
                var now = DateTime.UtcNow;
                p.Realm = 0; p.Stage = 0; p.Qi = 0; p.Bonus = 0;
                p.Area = 1; p.AreaMax = 1; p.Chapter = 0; p.ChapterMax = 0; p.BossKills = 0;
                p.BreakCooldownUntil = p.DebuffUntil = DateTime.MinValue;
                p.Skills = ""; p.SkillSlots = ""; p.AdminPoints = 0;
                p.AlchemyExp = 0; p.RefineRecipe = -1; p.RefineLeft = 0; p.RefineCatalyst = false;
                p.RefineStartAt = p.RefineEndAt = now; p.LastRefine = ""; p.LastRefineAt = DateTime.MinValue;
                p.BodyHp = 0; p.BodyDef = 0;
                p.LastTickAt = now;
                foreach (var e in p.Equipment.ToList()) { p.Equipment.Remove(e); db.Equipment.Remove(e); }
                // giữ lại dòng của vật phẩm khởi đầu (tránh EF theo dõi trùng khóa khi xóa rồi thêm lại), xóa phần còn lại
                foreach (var i in p.Inventory.Where(i => !GameData.StartingInventory.ContainsKey(i.ItemId)).ToList()) { p.Inventory.Remove(i); db.Inventory.Remove(i); }
                foreach (var i in p.Inventory) i.Count = 0;
                foreach (var (item, cnt) in GameData.StartingInventory) p.Add(item, cnt);
                p.Logs.Clear();
                db.Logs.Where(l => l.PlayerId == p.Id).ExecuteDelete();
                p.ExploreReadyAt = now.AddSeconds(ExploreSeconds(p));
                AddLog(p, "Đạo tâm vừa khởi. Nhân vật sẽ tự đi thám hiểm, nhặt linh tài và tích tu vi.");
                return Log(p, "[Admin] Đã reset nhân vật về từ đầu.", "gold");
            }
            case "unlock":
            {
                // mở hết ải chương đang đánh; đang ở chương cao nhất đã mở và đã mở hết ải thì mở thêm chương kế
                var c = Chap(p);
                if (p.Chapter == p.ChapterMax && p.AreaMax >= GameData.Areas && p.ChapterMax + 1 < GameData.OpenChapters) { p.ChapterMax++; p.AreaMax = 1; p.Chapter = p.ChapterMax; p.Area = 1; return Log(p, $"[Admin] Mở chương {p.ChapterMax + 1} · {Chap(p).Name}.", "good"); }
                if (p.Chapter == p.ChapterMax) p.AreaMax = GameData.Areas;
                p.Add(c.KeyItem, 1);
                return Log(p, $"[Admin] Mở toàn bộ ải {c.Index + 1}-1 → {c.Index + 1}-{GameData.Areas}, +1 {GameData.ItemById[c.KeyItem].Name}.", "good");
            }
            case "explore":
                p.ExploreReadyAt = DateTime.UtcNow;
                return Log(p, "[Admin] Chuyến thám hiểm kết thúc ngay.", "good");
            case "flag":
            {
                if (arg is null || !AdminFlags.Contains(arg)) throw new GameException("Cờ admin không hợp lệ.");
                var set = p.Admin.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var on = set.Add(arg); if (!on) set.Remove(arg);
                var fast = arg.StartsWith("fast");
                if (fast && on) set.RemoveWhere(f => f.StartsWith("fast") && f != arg);   // ×2 / ×3 / ×5: chỉ bật một mức
                p.Admin = string.Join(',', set);
                if (fast) p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
                var name = arg == "god" ? "Bất tử" : arg == "break" ? "Đột phá luôn thành công" : arg == "boss" ? "Đánh Boss không cần chìa khóa"
                    : $"Thám hiểm nhanh ×{(arg == "fast" ? "3" : arg[4..])}";
                return Log(p, $"[Admin] {name}: {(on ? "BẬT" : "TẮT")}.", on ? "good" : "");
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
                : slot == SkillData.BasicSlot ? "Ô đòn thường chỉ đặt đòn đánh thường (Hỏa Cầu, Thủy Cầu)." : $"{s.Name} là đòn đánh thường, đặt ở ô Đòn thường.");
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

    /// <summary>Bị động cho chiêu thức: % sát thương chiêu hệ Hỏa, % xuyên thấu phòng thủ quái, % công thiêu đốt mỗi lượt.</summary>
    private static (double skill, double pen, double burn) SkillExtra(Player p)
    {
        double k = 0, pe = 0, bu = 0;
        foreach (var (id, l) in SkillLevels(p))
            if (SkillData.ById.TryGetValue(id, out var s) && s.Tree != "basic") { k += s.SkillPct * s.Factor(l); pe += s.Pen * s.Factor(l); bu += s.BurnPct * s.Factor(l); }
        return (k, Math.Min(80, pe), bu);
    }

    // ================= Đan dược: hợp thành 9 → 1 =================

    /// <summary>Hợp thành n lần (0 = tối đa): mỗi lần 9 viên bậc dưới + phí linh thạch → 1 viên bậc trên. Luôn thành công.</summary>
    public (string msg, string kind, CraftResultDto craft) Synthesize(Player p, string fromId, int n)
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
        var (msg, kind) = Log(p, $"Hợp thành {sd.Count * n} {GameData.ItemById[sd.From].Name} → {n} {GameData.ItemById[sd.To].Name}.", "gold");
        var inputs = new List<ItemQtyDto> { new(sd.From, sd.Count * n) };
        if (sd.Stones > 0) inputs.Add(new("linh_thach", sd.Stones * n));
        return (msg, kind, new CraftResultDto($"{sd.From}>{sd.To}", new(sd.To, n), inputs));
    }

    private static int MaxSynth(Player p, SynthDef sd) =>
        Math.Min(p.Count(sd.From) / sd.Count, sd.Stones > 0 ? p.Count("linh_thach") / sd.Stones : int.MaxValue);

    // ================= Luyện khí: ghép 9 trang bị cùng phẩm → 1 phẩm cao hơn =================

    /// <summary>Ghép đúng GameData.FuseCount trang bị trong túi (items: danh sách id, lặp lại khi lấy nhiều món cùng id), cùng một phẩm.
    /// Thành công (GameData.FuseChance[phẩm]): 1 món phẩm kế tiếp; thất bại: 1 món cùng phẩm. Loại món gieo ngẫu nhiên trong các món đã bỏ vào.</summary>
    public (string msg, string kind, CraftResultDto craft) Fuse(Player p, string? items)
    {
        var ids = (items ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (ids.Count != GameData.FuseCount) throw new GameException($"Cần đúng {GameData.FuseCount} trang bị để luyện khí.");
        var defs = ids.Select(id => GameData.ItemById.TryGetValue(id, out var d) && d.Slot is not null && d.Base is not null ? d
            : throw new GameException("Chỉ ghép được trang bị.")).ToList();
        var q = Array.IndexOf(GameData.RarityOrder, defs[0].Rarity);
        if (defs.Any(d => d.Rarity != defs[0].Rarity)) throw new GameException("Các trang bị phải cùng phẩm.");
        if (q < 0 || q >= GameData.FuseChance.Length) throw new GameException($"{GameData.QualityName[^1]} đã là phẩm cao nhất.");
        foreach (var g in ids.GroupBy(x => x))
            if (p.Count(g.Key) < g.Count()) throw new GameException($"Không đủ {GameData.ItemById[g.Key].Name} trong hành trang.");
        foreach (var g in ids.GroupBy(x => x)) p.Add(g.Key, -g.Count());
        var rng = Random.Shared;
        var ok = rng.NextDouble() < GameData.FuseChance[q];
        var baseDef = GameData.GearBaseById[defs[rng.Next(defs.Count)].Base!];
        var outId = GameData.GearId(baseDef, ok ? q + 1 : q);
        p.Add(outId, 1);
        var label = GameData.Label(GameData.ItemById[outId]);
        var (msg, kind) = ok
            ? Log(p, $"Luyện khí thành công: {GameData.FuseCount} trang bị {GameData.QualityName[q]} → {label}.", "gold")
            : Log(p, $"Luyện khí thất bại: {GameData.FuseCount} trang bị {GameData.QualityName[q]} chỉ còn lại {label}.", "bad");
        var inputs = ids.GroupBy(x => x).Select(g => new ItemQtyDto(g.Key, g.Count())).ToList();
        return (msg, kind, new CraftResultDto(ok ? "fuse:ok" : "fuse:fail", new(outId, 1), inputs));
    }

    // ================= Bản đồ: 10 chương × (15 ải + ải Boss) =================

    /// <summary>Chọn ải để tự động thám hiểm trong chương đang đánh. Ải Boss (BossArea) cần lệnh Boss và đã mở ải cuối.</summary>
    public (string msg, string kind) SelectArea(Player p, int area)
    {
        var c = Chap(p); var C = c.Index + 1;
        if (area == GameData.BossArea)
        {
            if (MapAreaMax(p) < GameData.Areas) throw new GameException($"Mở ải {C}-{GameData.Areas} trước khi khiêu chiến Boss.");
            if (p.Count(c.KeyItem) < 1 && !p.Cheat("boss")) throw new GameException($"Cần {GameData.ItemById[c.KeyItem].Name} để vào {c.BossLair}. Rơi ở mọi ải thường của chương.");
        }
        else if (area < 1 || area > MapAreaMax(p)) throw new GameException("Ải này chưa mở.");
        if (p.Area == area) return ("", "");
        p.Area = area;
        p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));   // bắt đầu chuyến mới ở ải vừa chọn
        return area == GameData.BossArea
            ? Log(p, p.Count(c.KeyItem) > 0 ? $"Cầm {GameData.ItemById[c.KeyItem].Name} tiến vào {c.BossLair}, khiêu chiến {c.Boss.Name}!" : $"[Admin] Vào {c.BossLair} không cần lệnh, khiêu chiến {c.Boss.Name}!", "gold")
            : ($"Chuyển sang ải {C}-{area} · {GameData.AreaPlace(c, area)}.", "");
    }

    /// <summary>Chọn chương (0-based) đã mở: chương đã qua mở hết ải; vào ải cao nhất đã mở của chương đó.</summary>
    public (string msg, string kind) SelectMap(Player p, int chapter)
    {
        if (chapter < 0 || chapter >= GameData.Chapters.Count || !GameData.Chapters[chapter].Enabled) throw new GameException("Chương này tạm thời chưa mở.");
        if (chapter > p.ChapterMax) throw new GameException($"Đánh bại Boss chương {chapter} để mở chương {chapter + 1}.");
        if (p.Chapter == chapter) return ("", "");
        p.Chapter = chapter;
        p.Area = MapAreaMax(p);
        p.ExploreReadyAt = DateTime.UtcNow.AddSeconds(ExploreSeconds(p));
        var c = Chap(p);
        return Log(p, $"Chuyển sang chương {chapter + 1} · {c.Name}: ải {chapter + 1}-{p.Area} · {GameData.AreaPlace(c, p.Area)}.", "");
    }

    private static MapDto MapState(Player p)
    {
        var c = Chap(p); var amax = MapAreaMax(p);
        var areas = Enumerable.Range(1, GameData.BossArea).Select(a =>
        {
            var b = BattleAt(p, a);
            var ws = GameData.AreaWaves(c, a);
            return new AreaDto(a, b.Monster, GameData.AreaPlace(c, a),
                b.MonsterHp, b.MonsterAtk, b.MonsterDef, b.Win, b.Hits, a <= amax || (a == GameData.BossArea && amax >= GameData.Areas), b.Image,
                ws.Select(w => { var (lo, hi) = GameData.StoneRange(c, a, w.Tier); return new WaveInfoDto(w.Name, w.Image, w.Tier, w.Hp, w.Atk, w.Def, lo, hi); }).ToList(),
                ws.Any(w => w.Tier == "elite"));
        }).ToList();
        return new MapDto(p.Area, amax, c.KeyItem, c.BossItem ?? "", c.BossLair, GameData.KeyChance(1), GameData.KeyChance(GameData.Areas), areas,
            GameData.Synths.Select(sd => new SynthStateDto(sd.From, MaxSynth(p, sd))).ToList(), c.BossItem is not null && p.Count(c.BossItem) > 0, c.Index,
            c.Drops.Select(d => new DropInfoDto(d.Item, d.Min, d.Max, d.Chance, GameData.GearBaseById.ContainsKey(d.Item))).ToList(),
            GameData.ChestChance, GameData.RareChestChance, GameData.RareChestMinArea, GameData.BossChest,
            p.ChapterMax, c.Boss.Name, c.Boss.Image, c.Quality);
    }

    /// <summary>Quy đổi vật phẩm của bản cũ (3 loại linh thạch, nguyên liệu luyện đan, đan phẩm chất…).</summary>
    private static void ConvertLegacy(Player p)
    {
        foreach (var inv in p.Inventory.Where(i => i.Count > 0 && !GameData.ItemById.ContainsKey(i.ItemId)).ToList())
        {
            var id = inv.ItemId;
            var q = id.LastIndexOf("_q", StringComparison.Ordinal);
            if (GameData.UpgradeGear(id) is { } up) p.Add(up, inv.Count);                                      // trang bị bản cũ → đúng cấp, cùng phẩm
            else if (q > 0 && GameData.ItemById.ContainsKey(id[..q])) p.Add(id[..q], inv.Count);              // đan phẩm chất → đan gốc
            else if (q > 0 && GameData.Legacy.TryGetValue(id[..q], out var lq)) p.Add(lq.To, inv.Count * lq.Factor);
            else if (GameData.Legacy.TryGetValue(id, out var l)) p.Add(l.To, inv.Count * l.Factor);
            inv.Count = 0;
        }
        p.RefineRecipe = -1;
        // bản đồ theo cảnh giới (bản cũ) → bản đồ 10 chương: mọi tu sĩ bắt đầu lại từ ải 1-1
        if (p.MapVer < 1) { p.Chapter = p.ChapterMax = 0; p.Area = p.AreaMax = 1; p.MapRealm = -1; p.MapVer = 1; p.ExploreReadyAt = DateTime.UtcNow; }
        p.ChapterMax = Math.Clamp(p.ChapterMax, 0, GameData.OpenChapters - 1);
        p.Chapter = Math.Clamp(p.Chapter, 0, p.ChapterMax);
        p.Area = Math.Clamp(p.Area, 1, GameData.BossArea);
        p.AreaMax = Math.Clamp(p.AreaMax, 1, GameData.Areas);
        if (p.Area < GameData.BossArea) p.Area = Math.Min(p.Area, MapAreaMax(p));
        // bỏ kỳ "Đại viên mãn" (Trúc Cơ, Kim Đan còn 3 kỳ): tu sĩ đang ở kỳ cũ cuối → Hậu kỳ, giữ tu vi tối đa đủ đột phá
        if (p.Stage > Realm(p).Stages.Length - 1) { p.Stage = Realm(p).Stages.Length - 1; p.Qi = Math.Min(p.Qi, QiReq(p)); }
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
        var bossWins = 0;
        string? lostTo = null;                                      // quái (đợt) đã khiến chuyến thua                                            // số lần hạ Boss trong lượt cập nhật này (khiêu chiến liên tục đến hết chìa khóa)
        var startArea = p.Area;
        var mapTag = p.Chapter + 1;
        // linh thạch rơi khi hạ từng quái: thắng = mọi đợt, thua = các đợt đã hạ trước đợt cuối
        void AddStones(BattleDto bt, bool win)
        {
            var ws = bt.Waves ?? new List<WaveDto>();
            var n = ws.Take(win ? ws.Count : Math.Max(0, ws.Count - 1)).Sum(w => w.Stones);
            if (n <= 0) return;
            var got = TryAddLoot(p, "linh_thach", n);
            if (got > 0) loot["linh_thach"] = loot.GetValueOrDefault("linh_thach") + got;
        }
        var startChapter = p.Chapter;
        {
            var r = Chap(p);
            var t = p.ExploreReadyAt < now.AddSeconds(-GameData.MaxOfflineSeconds) ? now.AddSeconds(-GameData.MaxOfflineSeconds) : p.ExploreReadyAt;
            while (t <= now)
            {
                var area = p.Area;
                var battle = BattleAt(p, area, t);
                trips++;
                place = GameData.AreaPlace(r, area);
                if (area == GameData.BossArea)
                {
                    // ải Boss: mỗi lần khiêu chiến tốn 1 lệnh (thắng hay thua). Thắng: rương Boss (+ nguyên liệu đột phá ở chương 1–3),
                    // lần đầu hạ Boss chương cao nhất thì mở chương kế và chuyển sang ải 1 của chương đó; còn lệnh thì khiêu chiến tiếp.
                    var freeBoss = p.Cheat("boss") && p.Count(r.KeyItem) < 1;            // admin: không cần lệnh (không tiêu hao, đánh liên tục)
                    if (!freeBoss && p.Count(r.KeyItem) < 1) { p.Area = GameData.Areas; t = t.AddSeconds(ExploreSeconds(p)); continue; }
                    if (!freeBoss) p.Add(r.KeyItem, -1);
                    var keysLeft = p.Count(r.KeyItem);
                    var keyName = GameData.ItemById[r.KeyItem].Name;
                    if (battle.Win)
                    {
                        bossWins++;
                        AddStones(battle, true);
                        if (r.BossItem is not null) p.Add(r.BossItem, 1);
                        p.BossKills++;
                        p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) * 5);
                        var chestOk = TryAddLoot(p, GameData.BossChest, 1) > 0;   // Boss chắc chắn rơi rương
                        if (chestOk) loot[GameData.BossChest] = loot.GetValueOrDefault(GameData.BossChest) + 1;
                        var times = bossWins > 1 ? $" (lần {bossWins})" : "";
                        var gotText = string.Join(", ", new[] { r.BossItem is null ? null : GameData.ItemById[r.BossItem].Name, chestOk ? GameData.ItemById[GameData.BossChest].Name : null }.Where(x => x is not null));
                        special = $"Đánh bại {r.Boss.Name} ở {r.BossLair}{times}!{(gotText.Length > 0 ? $" Nhận {gotText}." : "")}{(chestOk ? "" : " Hành trang đầy, bỏ lại Rương Boss.")}"
                            + (freeBoss ? " [Admin: không cần lệnh]" : $" Tiêu hao {keyName}, còn {keysLeft}.");
                        specialKind = "gold";
                        // lần đầu hạ Boss của chương cao nhất: mở chương kế
                        if (r.Index == p.ChapterMax && p.ChapterMax + 1 < GameData.OpenChapters)
                        {
                            p.ChapterMax++; p.AreaMax = 1; p.Chapter = p.ChapterMax; p.Area = 1;
                            var nc = Chap(p);
                            special += $" Mở chương {nc.Index + 1} · {nc.Name}!";
                            t = t.AddSeconds(ExploreSeconds(p));
                            break;                                                   // sang chương mới: các chuyến sau tính ở lượt cập nhật kế
                        }
                        if (keysLeft > 0 || p.Cheat("boss")) { t = t.AddSeconds(ExploreSeconds(p)); continue; }   // còn lệnh: ở lại ải Boss, đánh tiếp
                        special += $" Trở về ải {mapTag}-{GameData.Areas}.";
                    }
                    else
                    {
                        losses++;
                        special = $"Không địch nổi {r.Boss.Name}, rút khỏi {r.BossLair}{(freeBoss ? "" : $" (tiêu hao {keyName}, còn {keysLeft})")}. Tu luyện thêm rồi quay lại.";
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
                    AddStones(battle, false);                                    // quái đã hạ trước khi thua vẫn rơi linh thạch
                    lostTo = battle.Waves is { Count: > 0 } lw ? lw[^1].Name : battle.Monster;
                    p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p) / 2);
                    if (p.Area > 1) p.Area--;
                    continue;
                }
                var eliteTrip = area >= GameData.EliteMinArea;                // chuyến có tinh anh: linh thạch ×1.5, rương hiếm 20%
                var scale = (1 + (area - 1) * 0.05) * (eliteTrip ? GameData.EliteStoneMult : 1);
                AddStones(battle, true);                                          // linh thạch: rơi theo từng quái gục
                foreach (var d in r.Drops)
                {
                    var isStone = d.Item == "linh_thach";
                    if (isStone) continue;
                    if (Random.Shared.NextDouble() >= Math.Min(1, d.Chance * (isStone || d.Chance >= 1 ? 1 : 1 + (area - 1) * 0.03))) continue;
                    var n = (int)Math.Round(Random.Shared.Next(d.Min, d.Max + 1) * (isStone ? scale : 1));
                    // trang bị: cấp theo cảnh giới yêu cầu của món, cấp bậc theo GameData.DropQuality
                    var dropId = GameData.GearBaseById.ContainsKey(d.Item) ? GameData.RollGear(d.Item, r.Quality) : d.Item;
                    var got = TryAddLoot(p, dropId, n);
                    if (got > 0) loot[dropId] = loot.GetValueOrDefault(dropId) + got;
                    lost += n - got;
                }
                if (Random.Shared.NextDouble() < GameData.KeyChance(area) && TryAddLoot(p, r.KeyItem, 1) > 0)
                    loot[r.KeyItem] = loot.GetValueOrDefault(r.KeyItem) + 1;
                // rương: thường ở mọi ải, hiếm từ ải RareChestMinArea
                foreach (var (chest, chance) in new[] { ("ruong_thuong", GameData.ChestChance),
                             ("ruong_hiem", eliteTrip ? GameData.EliteRareChestChance : area >= GameData.RareChestMinArea ? GameData.RareChestChance : 0) })
                {
                    if (chance <= 0 || Random.Shared.NextDouble() >= chance) continue;
                    if (TryAddLoot(p, chest, 1) > 0) loot[chest] = loot.GetValueOrDefault(chest) + 1; else lost++;
                }
                p.Qi = Math.Min(QiReq(p), p.Qi + ExploreQi(p));
                // thắng ở ải cao nhất → mở và tiến sang ải kế
                if (p.Chapter == p.ChapterMax && area == p.AreaMax && p.AreaMax < GameData.Areas) { p.AreaMax++; p.Area = p.AreaMax; }
            }
            p.ExploreReadyAt = t;
        }

        var gained = Math.Floor(p.Qi - before);
        var qiText = p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy bình cảnh, cần đột phá" : $"+{gained:N0} tu vi";
        var lootText = string.Join(", ", loot.Select(kv => $"+{kv.Value} {GameData.Label(GameData.ItemById[kv.Key])}"))
            + (lost > 0 ? $"{(loot.Count > 0 ? ", " : "")}hành trang đầy nên bỏ lại {lost} món" : "");
        var gotEquip = loot.Keys.Any(k => GameData.ItemById[k].Slot is not null || GameData.ItemById[k].Type == "key");
        if (special is not null) AddLog(p, special, specialKind);
        var areaTag = $"ải {mapTag}-{startArea}";
        if (p.Chapter == startChapter && p.Area != startArea && startArea <= GameData.Areas && trips > 0 && gap <= 60)
            AddLog(p, p.Area > startArea ? $"Qua {areaTag}, mở ải {mapTag}-{p.Area} · {GameData.AreaPlace(Chap(p), p.Area)}." : $"Thua ở {areaTag}, lùi về ải {mapTag}-{p.Area}.", p.Area > startArea ? "good" : "bad");
        if (gap > 60)
            AddLog(p, trips > 0
                ? $"Vắng mặt {Math.Round(gap / 60)} phút: thám hiểm {trips} chuyến{(losses > 0 ? $" (thua {losses})" : "")}, {qiText}{(lootText.Length > 0 ? ", " + lootText : "")}."
                : $"Vắng mặt {Math.Round(gap / 60)} phút, thu được {gained:N0} tu vi.", "gold");
        else if (special is not null && trips == 1) { }
        else if (trips > 0 && losses == trips)
            AddLog(p, $"Thám hiểm {areaTag} · {place}: bị {lostTo ?? GameData.AreaMonster(GameData.Chapters[startChapter], startArea).Name} đánh trọng thương, phải rút lui. Cần thêm tấn công, máu hoặc phòng thủ.{(lootText.Length > 0 ? $" Nhặt được {lootText}." : "")}", "bad");
        else if (trips > 0)
            AddLog(p, $"Thám hiểm {areaTag} · {place}: hạ {WaveNames(GameData.Chapters[startChapter], startArea)}: {(p.Qi >= QiReq(p) && gained <= 0 ? "tu vi đã đầy" : $"+{gained:N0} tu vi")}{(lootText.Length > 0 ? ", " + lootText : "")}.", gotEquip ? "gold" : "loot");
    }

    /// <summary>Tên các đợt quái của ải, gộp loài trùng: "Yêu Lang ×2, Độc Xà" · tinh anh ghi kèm.</summary>
    private static string WaveNames(ChapterDef r, int area) =>
        string.Join(", ", GameData.AreaWaves(r, area).GroupBy(w => w.Name)
            .Select(g => g.First().Name + (g.Count() > 1 ? $" ×{g.Count()}" : "") + (g.First().Tier == "elite" ? " (tinh anh)" : "")));

    /// <summary>Tổng chỉ số % từ trang bị đang mặc.</summary>
    private static double Bonus(Player p, Func<ItemDef, double> stat) =>
        p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? stat(d) : 0);

    /// <summary>Tu vi mỗi giây đã tính trang bị (gồm Hấp linh).</summary>
    private static double Rate(Player p) =>
        GameData.QiRate(Realm(p), p.Stage) * (1 + (Bonus(p, i => i.QiPct) + SkillBonus(p).qi + GearEffects(p).Qi) / 100) * (DateTime.UtcNow < p.DebuffUntil ? GameData.DebuffRate : 1);

    /// <summary>Tổng hiệu ứng riêng của trang bị đang mặc, đã áp trần (xem plan "Hiệu ứng riêng theo cấp trang bị").</summary>
    public record GearFx(double Qi, double ExploreQi, double Pen, double SkillPct, double Rage, double Guard, double ExtraHit, double HpPct,
        double Dodge, double Regen, double Shield, double FirstHit, double Reflect, double Lightning, double LightningPct, double MonAtkDown,
        double CritBonus, int Thien, double SetPct);
    public static GearFx GearEffects(Player p)
    {
        var sum = new Dictionary<string, double>(); double boltPct = 0; var thien = 0; var red = 0;
        foreach (var e in p.Equipment)
        {
            if (!GameData.ItemById.TryGetValue(e.ItemId, out var d)) continue;
            if (d.Slot is not null && d.Rarity is "legendary" or "mythic") thien++;   // đồ Đỏ tính vào bộ Vàng
            if (d.Slot is not null && d.Rarity == "mythic") red++;
            foreach (var f in d.Effects ?? Array.Empty<ItemEffect>())
            {
                sum[f.Id] = sum.GetValueOrDefault(f.Id) + f.Value;
                if (f.Id == "thien_loi") boltPct = Math.Max(boltPct, f.Value2);
            }
        }
        var redK = 1 + GameData.RedBonus(red) / 100;                                 // Đỏ mốc 3 · 6 · 9: tăng giá trị mọi hiệu ứng
        double S(string id) => sum.GetValueOrDefault(id) * redK;
        var set = thien >= Realm(p).EquipSlots.Length ? GameData.ThienSetFull : thien >= 5 ? GameData.ThienSet5 : thien >= 3 ? GameData.ThienSet3 : 0;
        return new GearFx(S("hap_linh"), S("tu_linh"), S("pha_giap"), S("linh_ap"), Math.Min(75, S("tinh_tam")), S("ho_the"), Math.Min(30, S("lien_kich")),
            S("vung_than"), S("than_phap"), S("hoi_khi"), Math.Min(20, S("tho_thuan")), S("dia_chan"), Math.Min(15, S("dia_mach")),
            Math.Min(50, S("thien_loi")), boltPct, S("thien_uy"), S("thien_menh"), thien, set);
    }

    /// <summary>Thời lượng một chuyến: đi đường + số lượt đánh + kết thúc. Tấn công cao thì hạ quái nhanh hơn.</summary>
    private static double ExploreSeconds(Player p)
    {
        var t = Math.Max(GameData.MinExploreSeconds,
            GameData.WalkSeconds * (1 - Math.Min(60, Stats(p).Move) / 100)
            + Battle(p).Hits * GameData.HitSeconds * (1 - Math.Min(60, Stats(p).Speed) / 100)
            + (Math.Max(1, Battle(p).Waves?.Count ?? 1) - 1) * GameData.WaveSeconds
            + GameData.EndSeconds);
        var k = FastMult(p);                                   // admin: thám hiểm nhanh ×2 / ×3 / ×5
        return k > 1 ? Math.Max(1, t / k) : t;
    }

    /// <summary>Chỉ số chiến đấu = chỉ số gốc theo cảnh giới/tầng + trang bị.</summary>
    public static CombatStats Stats(Player p)
    {
        var b = GameData.BaseStats(Realm(p), p.Stage);
        int Sum(Func<ItemDef, int> f) => p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? f(d) : 0);
        double SumD(Func<ItemDef, double> f) => p.Equipment.Sum(e => GameData.ItemById.TryGetValue(e.ItemId, out var d) ? f(d) : 0);
        var baseSpeed = GameData.BaseSpeed[Math.Min(p.Realm, GameData.BaseSpeed.Length - 1)];
        bool On(string id) => p.Realm >= GameData.ExtraStats.First(x => x.Id == id).MinRealm;
        var weak = DateTime.UtcNow < p.DebuffUntil ? GameData.DebuffStat : 1;   // tẩu hỏa nhập ma
        var sk = SkillBonus(p);                                                  // công pháp bị động
        var fx = GearEffects(p);                                                 // Vững thân (máu) + thưởng bộ Thiên cấp
        return new CombatStats((int)((b.Atk + Sum(i => i.Atk)) * weak * (1 + (sk.atk + fx.SetPct) / 100)), (int)((b.Hp + Sum(i => i.Hp) + p.BodyHp) * (1 + (sk.hp + fx.HpPct + fx.SetPct) / 100)),
            (int)((b.Def + Sum(i => i.Def) + p.BodyDef) * weak * (1 + (sk.def + fx.SetPct) / 100)),
            (On("speed") ? baseSpeed + SumD(i => i.SpeedPct) : 0) + sk.speed,
            5 + SumD(i => i.CritPct) + sk.crit,
            SumD(i => i.MovePct) + sk.move);
    }

    /// <summary>
    /// Trận đánh theo lượt, không ngẫu nhiên: tu sĩ chém trước, quái phản đòn sau mỗi lượt.
    /// Thắng nếu hạ được quái trước khi hết máu.
    /// </summary>
    public static BattleDto Battle(Player p) => BattleAt(p, p.Area);

    /// <param name="trip">Thời điểm kết thúc chuyến (mặc định ExploreReadyAt): làm hạt giống ngẫu nhiên cố định
    /// để trận client đang diễn và trận server chốt kết quả ra cùng một kết quả (thiêu đốt 30%).</param>
    public static BattleDto BattleAt(Player p, int area, DateTime? trip = null)
    {
        var tripSec = (trip ?? p.ExploreReadyAt).Ticks / TimeSpan.TicksPerSecond;
        var rng = new Random(unchecked((p.Id.GetHashCode() * 31 + area) * 31 + (int)(tripSec % 1_000_000_007)));
        var stoneRng = new Random(unchecked((p.Id.GetHashCode() * 37 + area) * 41 + (int)(tripSec % 1_000_000_007)));   // linh thạch mỗi quái: cùng chuyến ra cùng số ở client và server
        var st = Stats(p);
        var fx = GearEffects(p);                               // hiệu ứng riêng của trang bị theo cấp
        var down = Math.Min(40, SkillBonus(p).monAtk + fx.MonAtkDown);   // công pháp khống chế + Thiên uy làm yếu đòn quái
        var (skillPct, pen, burnPct) = SkillExtra(p);                   // Hỏa Tụ / Liệt Diễm Tâm / Xích Diễm Đạo Thể · Hỏa Linh Chân Ý
        pen = Math.Min(80, pen + fx.Pen); skillPct += fx.SkillPct;      // Phá giáp · Linh áp
        // các đợt quái của ải (ải thường 3 đợt, ải Boss 1): đã trừ công (khống chế) và thủ (phá giáp)
        var waves = GameData.AreaWaves(Chap(p), area).Select(w => w with {
            Atk = down > 0 ? (int)Math.Round(w.Atk * (1 - down / 100)) : w.Atk,
            Def = pen > 0 ? (int)Math.Round(w.Def * (1 - pen / 100)) : w.Def }).ToArray();
        var wave = 0; var m = waves[0];
        var crit = Math.Min(100, st.Crit) / 100;
        var critMult = 0.5 + fx.CritBonus;                               // Thiên mệnh
        var power = st.Atk * (1 + crit * critMult);
        // đòn đánh thường = chiêu ở ô 1 (Hỏa Cầu / Thủy Cầu), chưa đặt thì đánh 100% công
        var lv = SkillLevels(p);
        var basicId = p.Slots()[SkillData.BasicSlot];
        var basicOk = SkillData.ById.TryGetValue(basicId, out var bdef) && bdef.Tree == "basic";
        if (!basicOk) { basicId = "hoa_cau"; bdef = SkillData.ById[basicId]; }        // mặc định: Hỏa Cầu
        var bcrit = Math.Min(100, st.Crit + bdef!.Crit) / 100;                       // đòn thường có thể cộng thêm chí mạng riêng
        var bpower = st.Atk * (1 + bcrit * critMult);
        var heroBase = (int)Math.Round(bpower * bdef.DmgBase * (1 + skillPct / 100) / 100);
        int HeroDmgVs(MonsterDef x) => Math.Max(1, heroBase - x.Def);
        var heroDmg = HeroDmgVs(m);

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
        shield = Math.Min(60, shield + fx.Guard); dodge = Math.Min(50, dodge + fx.Dodge); stun = Math.Min(4, stun);   // + Hộ thể · Thân pháp
        int MonHit(double atk) => p.Cheat("god") ? 0 : Math.Max(1, (int)Math.Round(Math.Max(1, atk - st.Def) * (1 - shield / 100) * (1 - dodge / 100)));   // admin: bất tử
        var monDmg = MonHit(m.Atk);
        // Boss: kỹ năng ẩn — Yêu thân (giảm sát thương nhận) · Cuồng bạo (máu thấp thì công tăng)
        var isBoss = area >= GameData.BossArea;
        var trait = isBoss ? Chap(p).Trait : null;
        double TakeK(MonsterDef x) => x.Tier == "elite" ? 1 - GameData.EliteDmgReduce / 100 : trait is null ? 1 : 1 - trait.DmgReduce / 100;   // tinh anh: giảm 5% sát thương nhận
        var takeK = TakeK(m);
        var monDmgEnr = trait is null ? monDmg : MonHit(m.Atk * (1 + trait.EnrageAtk / 100));
        var enraged = false; var enrageAt = -1;

        // thiêu đốt bị động: % công mỗi lượt, bỏ qua phòng thủ, được cộng % sát thương Hỏa
        var burnDmg = burnPct > 0 ? Math.Max(1, (int)Math.Round(power * burnPct / 100 * (1 + skillPct / 100))) : 0;
        // hiệu ứng trang bị: khiên đầu trận · đòn Địa chấn · tia Thiên lôi · hồi máu mỗi lượt
        var guard = (int)Math.Round(st.Hp * fx.Shield / 100); var guardLeft = guard;
        var firstHit = fx.FirstHit > 0 ? Math.Max(1, (int)Math.Round(power * fx.FirstHit / 100)) : 0;
        var boltDmg = fx.Lightning > 0 ? Math.Max(1, (int)Math.Round(power * fx.LightningPct / 100)) : 0;
        var regen = (int)Math.Round(st.Hp * fx.Regen / 100);
        int monHp = m.Hp, heroHp = st.Hp, turns = 0, rage = (int)fx.Rage, tw = 0; var win = false;   // Tĩnh tâm: vào trận đã có nộ · tw: lượt trong đợt
        var waveFirst = new List<int> { 0 };                                  // lượt (thật) bắt đầu mỗi đợt
        bool NextWave(int t)                                                  // hạ xong đợt này: quái đợt sau bước vào, máu tu sĩ giữ nguyên
        {
            if (wave + 1 >= waves.Length) return false;
            wave++; m = waves[wave]; heroDmg = HeroDmgVs(m); monDmg = MonHit(m.Atk); monDmgEnr = monDmg; takeK = TakeK(m);
            monHp = m.Hp; tw = -1; waveFirst.Add(t + 1);
            return true;
        }
        var monLeft = new List<int>(); var heroLeft = new List<int>(); var skip = new List<bool>(); var casts = new List<CastDto>(); var rages = new List<int>(); var burns = new List<bool>();
        var bolts = new List<bool>(); var extras = new List<bool>(); var heals = new List<int>(); var reflects = new List<int>(); var guards = new List<int>();
        // thứ tự mỗi lượt (giữ cố định để gieo số giống nhau): thiêu đốt → thiên lôi → đòn thường / kỹ năng → liên kích → quái đánh → phản đòn → hồi máu
        for (var t = 0; t < 150; t++, tw++)
        {
            var dmg = 0;
            if (burn > 0 && tw > 0) dmg += Math.Max(1, (int)Math.Round(power * burn / 100) - m.Def / 2);
            var burned = burnDmg > 0 && tw > 0 && rng.Next(100) < SkillData.BurnChance;   // Phần Cốt: 30% mỗi lượt, từ lượt 2 của mỗi đợt
            if (burned) dmg += burnDmg;
            burns.Add(burned);
            var bolt = boltDmg > 0 && rng.Next(100) < fx.Lightning;           // Thiên lôi
            if (bolt) dmg += boltDmg;
            bolts.Add(bolt);
            if (t == 0) dmg += firstHit;                                       // Địa chấn
            var full = rage >= SkillData.RageMax; var fired = false;          // đầu lượt đã đủ 100 nộ → lượt này tung kỹ năng chủ động
            foreach (var (s, l) in equipped)
            {
                var a = SkillData.Active(s, l);
                var pct = SkillData.Damage(s, l);
                if (pct == 0 || a.stun > 0 && a.burn > 0)
                {
                    if (t == 0) casts.Add(new CastDto(0, s.Id, s.Name, s.Anim, 0));   // chiêu thủ / khống / thân pháp: thi triển đầu trận
                    continue;
                }
                if (!full) continue;                                           // đủ 100 nộ mới tung kỹ năng chủ động
                fired = true;
                var cd = Math.Max(1, (int)Math.Round(power * pct * (1 + skillPct / 100) / 100) - m.Def);
                dmg += cd;
                casts.Add(new CastDto(t, s.Id, s.Name, s.Anim, cd));
            }
            if (fired) rage = 0;                                               // tung chiêu thay cho đòn thường, nộ về 0
            else { dmg += heroDmg; rage = Math.Min(SkillData.RageMax, rage + SkillData.RagePerHit); }   // đòn đánh thường tích nộ
            var extra = fx.ExtraHit > 0 && rng.Next(100) < fx.ExtraHit;      // Liên kích: thêm 1 đòn thường (không tích nộ)
            if (extra) dmg += heroDmg;
            extras.Add(extra);
            rages.Add(rage);
            if (takeK < 1) dmg = (int)Math.Round(dmg * takeK);                // Yêu thân
            monHp -= dmg; turns = t + 1;
            monLeft.Add(Math.Max(0, monHp));
            if (monHp <= 0) { heroLeft.Add(heroHp); skip.Add(true); heals.Add(0); reflects.Add(0); guards.Add(guardLeft); if (NextWave(t)) continue; win = true; break; }
            var stunned = tw < stun;                                            // khống: các lượt đầu của mỗi đợt
            int refl = 0, heal = 0;
            if (!stunned)
            {
                if (!enraged && trait is not null && trait.EnrageAtk > 0 && monHp < m.Hp * trait.EnrageAt / 100) { enraged = true; enrageAt = t; }   // Cuồng bạo
                var hit = enraged ? monDmgEnr : monDmg;
                if (guardLeft > 0) { var ab = Math.Min(guardLeft, hit); guardLeft -= ab; hit -= ab; }   // Thổ thuẫn đỡ trước
                heroHp -= hit;
                var dealt = enraged ? monDmgEnr : monDmg;
                if (heroHp > 0 && fx.Reflect > 0 && dealt > 0) { refl = Math.Max(1, (int)Math.Round(dealt * fx.Reflect / 100 * takeK)); monHp -= refl; monLeft[^1] = Math.Max(0, monHp); }   // Địa mạch
            }
            if (heroHp > 0 && regen > 0) { heal = Math.Min(regen, st.Hp - heroHp); heroHp += heal; }   // Hồi khí
            heroLeft.Add(Math.Max(0, heroHp)); skip.Add(stunned); heals.Add(heal); reflects.Add(refl); guards.Add(guardLeft);
            if (heroHp <= 0) break;
            if (monHp <= 0) { if (NextWave(t)) continue; win = true; break; }   // quái gục vì phản đòn
        }
        // hoạt ảnh tối đa MaxHits lượt: trận dài hơn thì lấy mẫu đều các lượt
        // (ải 3 đợt: mỗi đợt tối đa MaxWaveHits lượt, luôn giữ lượt hạ gục để client đổi quái đúng lúc)
        LastTurns = turns;
        var cap = waves.Length > 1 ? GameData.MaxWaveHits : GameData.MaxHits;
        var idxL = new List<int>(); var waveDtos = new List<WaveDto>();
        for (var w = 0; w < waveFirst.Count; w++)
        {
            int s0 = waveFirst[w], n = (w + 1 < waveFirst.Count ? waveFirst[w + 1] : turns) - s0;
            if (n <= 0) break;
            var h = Math.Min(cap, n); var start = idxL.Count;
            for (var i = 0; i < h; i++) idxL.Add(s0 + (n <= h ? i : (int)Math.Round((i + 1) * (double)n / h) - 1));
            var wm = waves[w];
            var (slo, shi) = GameData.StoneRange(Chap(p), area, wm.Tier);
            waveDtos.Add(new WaveDto(wm.Name, wm.Image, wm.Tier, wm.Hp, wm.Atk, wm.Def, HeroDmgVs(wm), MonHit(wm.Atk), start, shi > 0 ? stoneRng.Next(slo, shi + 1) : 0));
        }
        var idx = idxL.ToArray(); var hits = idx.Length; var m0 = waves[0];
        var castShown = casts.Where(c => idx.Contains(c.Turn)).Select(c => c with { Turn = Array.IndexOf(idx, c.Turn) }).ToList();
        return new BattleDto(m0.Name, m0.Image, m0.Hp, m0.Atk, m0.Def, waveDtos[0].HeroDmg, waveDtos[0].MonsterDmg, hits, win,
            idx.Select(i => monLeft[i]).ToArray(), idx.Select(i => heroLeft[i]).ToArray(), idx.Select(i => skip[i]).ToArray(), castShown, st.Hp, basicId,
            idx.Select(i => rages[i]).ToArray(), burnDmg, Math.Round(burnPct, 1), idx.Select(i => burns[i]).ToArray(),
            idx.Select(i => bolts[i]).ToArray(), boltDmg, idx.Select(i => extras[i]).ToArray(), idx.Select(i => heals[i]).ToArray(),
            idx.Select(i => reflects[i]).ToArray(), guard, idx.Select(i => guards[i]).ToArray(), firstHit,
            enrageAt < 0 ? -1 : Array.FindIndex(idx, i => i >= enrageAt), monDmgEnr, waveDtos);
    }
    /// <summary>Số lượt thật của trận vừa tính (công cụ mô phỏng cân bằng).</summary>
    [ThreadStatic] public static int LastTurns;

    private static double ExploreQi(Player p) => GameData.QiRate(Realm(p), p.Stage) * GameData.ExploreQiSeconds * (1 + GearEffects(p).ExploreQi / 100);   // + Tụ linh

    private static RealmDef Realm(Player p) => GameData.Realms[p.Realm];
    /// <summary>Chương đang đánh.</summary>
    private static ChapterDef Chap(Player p) => GameData.Chapters[Math.Clamp(p.Chapter, 0, GameData.Chapters.Count - 1)];
    /// <summary>Ải cao nhất đã mở trong chương đang đánh: chương đã qua mở hết ải.</summary>
    private static int MapAreaMax(Player p) => p.Chapter < p.ChapterMax ? GameData.Areas : p.AreaMax;
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
