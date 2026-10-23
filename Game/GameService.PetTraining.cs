using VanDao.Data;
namespace VanDao.Game;

/// <summary>Bồi dưỡng linh thú: tẩy luyện / bồi nguyên tư chất, trang bị linh thú, thức tỉnh huyết mạch, cộng hưởng ngũ hành.</summary>
public partial class GameService
{
    // ================= Chỉ số =================
    private static string GearOf(Pet pet, string slot) => slot switch { "collar" => pet.GearCollar, "armor" => pet.GearArmor, "charm" => pet.GearCharm, _ => "" };
    private static void SetGear(Pet pet, string slot, string item)
    {
        switch (slot) { case "collar": pet.GearCollar = item; break; case "armor": pet.GearArmor = item; break; case "charm": pet.GearCharm = item; break; default: throw new GameException("Ô trang bị linh thú không hợp lệ."); }
    }
    /// <summary>% cộng thêm vào một chỉsố (atk · def · hp) từ trang bị linh thú đang mặc.</summary>
    private static double PetGearPct(Pet pet, string stat) => PetTraining.Slots.Where(s => s.Stat == stat)
        .Sum(s => PetTraining.Gear.TryGetValue(GearOf(pet, s.Id), out var g) ? g.Pct : 0);
    private static (double Pct, string Kind) PetResonance(Player p, Pet pet) =>
        PetData.ById.TryGetValue(pet.DefId, out var d) ? PetTraining.Resonance(d.Element, CultivationManuals.Active(p)) : (0, "");
    /// <summary>Tháo mọi trang bị linh thú về túi (phóng sinh / làm con hiến), không chặn khi túi đầy để không mất đồ.</summary>
    private static int StripPetGear(Player p, Pet pet)
    {
        var n = 0;
        foreach (var s in PetTraining.Slots)
        {
            var item = GearOf(pet, s.Id);
            if (item.Length == 0) continue;
            if (GameData.ItemById.ContainsKey(item)) { p.Add(item, 1); n++; }
            SetGear(pet, s.Id, "");
        }
        return n;
    }

    // ================= Trạng thái gửi giao diện =================
    private static readonly PetTrainingRules TrainingRules = new(
        PetTraining.Slots.Select(s => new PetGearSlotDto(s.Id, s.Name, s.Stat, s.StatName)).ToArray(),
        PetTraining.TierNames,
        PetTraining.Gear.Values.Select(g => new PetGearInfoDto(g.Id, g.Slot, g.Tier, g.Pct)).ToArray(),
        PetTraining.FuseCount, Enumerable.Range(0, PetTraining.TierNames.Length).Select(PetTraining.FuseStones).ToArray(),
        PetTraining.MaxLocks, PetTraining.TrainMin, PetTraining.TrainMax,
        PetTraining.MaxAwaken, PetTraining.AwakenBonus, PetTraining.ResonanceSame, PetTraining.ResonanceSupport);

    private static PetTrainingDto TrainingFor(Player p, Pet pet)
    {
        var d = PetData.ById[pet.DefId];
        var (lo, hi) = PetTraining.AptRange(pet, d);
        var lv = Math.Clamp(pet.Awaken, 0, PetTraining.MaxAwaken);
        var cost = PetTraining.AwakenCost(lv);
        var (res, kind) = PetResonance(p, pet);
        return new(lo, hi, PetTraining.RerollStones(PetData.QualityOf(pet, d)), lv, PetTraining.AwakenNames[lv],
            cost.Select(x => new CostDto(x.Key, x.Value, p.Count(x.Key))).ToArray(),
            lv < PetTraining.MaxAwaken ? PetTraining.AwakenDonorStars[lv] : -1, AwakenGate(pet), AwakenHint(p, pet),
            PetTraining.Slots.ToDictionary(s => s.Id, s => GearOf(pet, s.Id)),
            new() { ["atk"] = PetGearPct(pet, "atk"), ["def"] = PetGearPct(pet, "def"), ["hp"] = PetGearPct(pet, "hp") },
            res, kind, CultivationManuals.Active(p));
    }

    // ================= Tư chất =================
    /// <summary>Tẩy luyện: random lại các tư chất chưa khóa (locks = "atk,def"…, tối đa 2) trong khoảng của phẩm.</summary>
    public (string, string) RerollAptitude(Player p, Guid id, string? locks)
    {
        var pet = FindPet(p, id); var d = PetData.ById[pet.DefId];
        var keep = (locks ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToHashSet();
        if (keep.Any(k => k is not ("atk" or "def" or "hp"))) throw new GameException("Tư chất khóa không hợp lệ.");
        if (keep.Count > PetTraining.MaxLocks) throw new GameException($"Chỉ khóa được tối đa {PetTraining.MaxLocks} tư chất.");
        var pills = PetTraining.RerollPills(keep.Count); var stones = PetTraining.RerollStones(PetData.QualityOf(pet, d));
        if (p.Count(PetTraining.RerollItem) < pills) throw new GameException($"Cần {pills} Hoán Cốt Đan (1 viên + 1 viên mỗi tư chất khóa).");
        if (p.Count("linh_thach") < stones) throw new GameException($"Cần {stones:N0} linh thạch.");
        var (lo, hi) = PetTraining.AptRange(pet, d);
        var before = (pet.AptAtk, pet.AptDef, pet.AptHp);
        if (!keep.Contains("atk")) pet.AptAtk = Random.Shared.Next(lo, hi + 1);
        if (!keep.Contains("def")) pet.AptDef = Random.Shared.Next(lo, hi + 1);
        if (!keep.Contains("hp")) pet.AptHp = Random.Shared.Next(lo, hi + 1);
        p.Add(PetTraining.RerollItem, -pills); p.Add("linh_thach", -stones);
        string Diff(string n, int a, int b) => a == b ? $"{n} {b}" : $"{n} {a}→{b}";
        return ($"Tẩy luyện {d.Name}: {Diff("Công", before.AptAtk, pet.AptAtk)} · {Diff("Thủ", before.AptDef, pet.AptDef)} · {Diff("Huyết", before.AptHp, pet.AptHp)}.", "good");
    }

    /// <summary>Bồi nguyên: mỗi viên +1~3 vào một tư chất, dừng khi chạm trần của phẩm (n = số viên, 0 = tới trần).</summary>
    public (string, string) TrainAptitude(Player p, Guid id, string? stat, int n)
    {
        var pet = FindPet(p, id); var d = PetData.ById[pet.DefId];
        if (stat is not ("atk" or "def" or "hp")) throw new GameException("Chọn tư chất Công, Thủ hoặc Huyết.");
        if (n is not (0 or 1 or 10)) throw new GameException("Chọn 1, 10 hoặc tới trần.");
        var (_, hi) = PetTraining.AptRange(pet, d);
        int Get() => stat == "atk" ? pet.AptAtk : stat == "def" ? pet.AptDef : pet.AptHp;
        void Set(int v) { if (stat == "atk") pet.AptAtk = v; else if (stat == "def") pet.AptDef = v; else pet.AptHp = v; }
        if (Get() >= hi) throw new GameException($"Tư chất này đã đạt trần {hi} của phẩm.");
        var have = p.Count(PetTraining.TrainItem);
        if (have < 1) throw new GameException("Không có Bồi Linh Đan.");
        var limit = n == 0 ? have : Math.Min(n, have); var used = 0; var from = Get();
        while (used < limit && Get() < hi) { used++; Set(Math.Min(hi, Get() + Random.Shared.Next(PetTraining.TrainMin, PetTraining.TrainMax + 1))); }
        p.Add(PetTraining.TrainItem, -used);
        var name = stat == "atk" ? "Công" : stat == "def" ? "Thủ" : "Huyết";
        return ($"Bồi nguyên {used} viên: tư chất {name} {from}→{Get()}{(Get() >= hi ? " (đã đạt trần)" : "")}.", "good");
    }

    // ================= Trang bị linh thú =================
    public (string, string) EquipPetGear(Player p, Guid id, string itemId)
    {
        var pet = FindPet(p, id);
        if (!PetTraining.Gear.TryGetValue(itemId, out var g)) throw new GameException("Đây không phải trang bị linh thú.");
        if (p.Count(itemId) < 1) throw new GameException("Không có trang bị này trong hành trang.");
        var old = GearOf(pet, g.Slot);
        if (old == itemId) throw new GameException("Linh thú đang mặc đúng món này.");
        if (old.Length > 0 && p.Count(old) == 0 && p.Count(itemId) > 1 && BagUsed(p) >= BagCap(p)) throw new GameException("Hành trang đầy, không tháo được món đang mặc.");
        p.Add(itemId, -1);
        if (old.Length > 0 && GameData.ItemById.ContainsKey(old)) p.Add(old, 1);
        SetGear(pet, g.Slot, itemId);
        return ($"{PetData.ById[pet.DefId].Name} mặc {GameData.ItemById[itemId].Name}.", "good");
    }

    public (string, string) UnequipPetGear(Player p, Guid id, string slot)
    {
        var pet = FindPet(p, id);
        if (PetTraining.Slots.All(s => s.Id != slot)) throw new GameException("Ô trang bị linh thú không hợp lệ.");
        var old = GearOf(pet, slot);
        if (old.Length == 0) throw new GameException("Ô này đang trống.");
        if (p.Count(old) == 0 && BagUsed(p) >= BagCap(p)) throw new GameException("Hành trang đầy.");
        if (GameData.ItemById.ContainsKey(old)) p.Add(old, 1);
        SetGear(pet, slot, "");
        return ($"Đã tháo {GameData.ItemById.GetValueOrDefault(old)?.Name ?? old}.", "good");
    }

    /// <summary>Ghép 3 món cùng ô, cùng phẩm → 1 món phẩm kế (n = số lần, 0 = tối đa).</summary>
    public (string msg, string kind, CraftResultDto? craft) FusePetGear(Player p, string itemId, int n)
    {
        if (!PetTraining.Gear.TryGetValue(itemId, out var g)) throw new GameException("Đây không phải trang bị linh thú.");
        if (g.Tier >= PetTraining.TierNames.Length - 1) throw new GameException("Đã là phẩm cao nhất.");
        var stones = PetTraining.FuseStones(g.Tier);
        var max = Math.Min(p.Count(itemId) / PetTraining.FuseCount, stones > 0 ? (int)Math.Min(int.MaxValue, p.Count("linh_thach") / stones) : int.MaxValue);
        if (p.Count(itemId) < PetTraining.FuseCount) throw new GameException($"Cần {PetTraining.FuseCount} món {GameData.ItemById[itemId].Name}.");
        if (max < 1) throw new GameException($"Cần {stones:N0} linh thạch mỗi lần ghép.");
        var times = n <= 0 ? max : Math.Min(n, max);
        var to = PetTraining.GearId(g.Slot, g.Tier + 1);
        if (p.Count(to) == 0 && p.Count(itemId) - times * PetTraining.FuseCount > 0 && BagUsed(p) >= BagCap(p)) throw new GameException("Hành trang đầy.");
        p.Add(itemId, -times * PetTraining.FuseCount); p.Add("linh_thach", -stones * times); p.Add(to, times);
        var msg = Log(p, $"Ghép trang bị linh thú: {times * PetTraining.FuseCount} {GameData.ItemById[itemId].Name} → {times} {GameData.ItemById[to].Name}.", "good");
        return (msg.Item1, msg.Item2, new CraftResultDto("petgear", new ItemQtyDto(to, times),
            new() { new(itemId, times * PetTraining.FuseCount), new("linh_thach", stones * times) }, null));
    }

    // ================= Thức tỉnh huyết mạch =================
    private static bool AwakenGate(Pet pet) => PetData.StarsOf(pet) >= PetData.MaxStars && pet.Awaken < PetTraining.MaxAwaken;
    private static string AwakenHint(Player p, Pet pet)
    {
        if (pet.Awaken >= PetTraining.MaxAwaken) return $"Đã thức tỉnh tối đa (+{PetTraining.MaxAwaken * PetTraining.AwakenBonus * 100:0}% chỉ số).";
        if (PetData.StarsOf(pet) < PetData.MaxStars) return $"Cần {PetData.MaxStars}★ để thức tỉnh huyết mạch.";
        var stars = PetTraining.AwakenDonorStars[pet.Awaken];
        var missing = new List<string>();
        if (!p.Pets.Any(x => CanAwakenDonor(pet, x))) missing.Add($"1 {PetData.ById[pet.DefId].Name} {stars}★ khác đang nghỉ");
        if (!p.Has(PetTraining.AwakenCost(pet.Awaken))) missing.Add("đủ nguyên liệu");
        return missing.Count > 0 ? "Cần " + string.Join(" · ", missing) + "." : "Đã đủ điều kiện thức tỉnh.";
    }
    private static bool CanAwakenDonor(Pet pet, Pet donor) => pet.Awaken < PetTraining.MaxAwaken && donor.Id != pet.Id && donor.DefId == pet.DefId && donor.Slot == 0
        && PetData.StarsOf(donor) >= PetTraining.AwakenDonorStars[pet.Awaken];

    public (string, string) AwakenPet(Player p, Guid id, Guid donorId)
    {
        var pet = FindPet(p, id);
        if (!AwakenGate(pet)) throw new GameException(AwakenHint(p, pet));
        var donor = FindPet(p, donorId);
        if (!CanAwakenDonor(pet, donor)) throw new GameException($"Con hiến phải cùng loài, từ {PetTraining.AwakenDonorStars[pet.Awaken]}★ và đang nghỉ.");
        var cost = PetTraining.AwakenCost(pet.Awaken);
        if (!p.Has(cost)) throw new GameException(AwakenHint(p, pet));
        p.Pay(cost);
        var back = donor.FoodSpent / 2; StripPetGear(p, donor);
        db.Pets.Remove(donor); p.Pets.Remove(donor);
        if (back > 0) p.Add("linh_thu_dan", back);
        pet.Awaken++;
        return Log(p, $"{PetData.ById[pet.DefId].Name} {PetTraining.AwakenNames[pet.Awaken]}: +{pet.Awaken * PetTraining.AwakenBonus * 100:0}% chỉ số{(back > 0 ? $", hoàn {back} Linh Thú Đan" : "")}.", "gold");
    }
}
