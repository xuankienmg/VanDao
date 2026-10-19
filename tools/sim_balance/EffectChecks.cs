using System.Globalization;
using VanDao.Game;
using VanDao.Data;
using Microsoft.EntityFrameworkCore;

static class EffectChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static ItemDef[] Bases(int realm) => GameData.GearBases.Where(b => b.MinRealm == realm && b.Set != null)
        .GroupBy(b => b.Slot).Select(g => g.First()).ToArray();
    static Player Player(int realm, int quality, int count = 10)
    {
        var p = new Player { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Realm = realm };
        foreach (var b in Bases(realm).Take(count)) p.Equipment.Add(new EquippedItem { Slot = b.Slot!, ItemId = GameData.GearId(b, quality) });
        return p;
    }
    public static void Run()
    {
        foreach (var realm in Enumerable.Range(0, 6)) foreach (var q in Enumerable.Range(0, 10))
        {
            foreach (var count in new[] { 0, 2, 3, 4, 5, 7, 8, 9, 10 })
            {
                var p = Player(realm, q, count);
                var snapshot = EquipmentEffects.Calculate(p);
                var set = snapshot.Sets.Single(s => s.Id == EquipmentEffects.Sets[realm].Id);
                Check(set.Count == count, $"Set count realm={realm} q={q}: wanted {count}, actual {set.Count}; items={string.Join(',', EquipmentEffects.ValidItems(p).Select(i => i.SetId))}");
                Check(set.Active.SequenceEqual(new[] { 3, 5, 8, 10 }.Where(n => count >= n)), "Milestones");
                Check(snapshot.SetBonuses.GetValueOrDefault("set_pct") == (count >= 10 ? 8 : count >= 3 ? 3 : 0), "Cumulative set bonus");
                Check(snapshot.Effects.All(e => e.Cap == null || e.Effective <= e.Cap), "Caps");
                var before = set.Active.Length;
                if (count > 0) p.Equipment.RemoveAt(p.Equipment.Count - 1);
                Check(EquipmentEffects.Calculate(p).Sets[realm].Active.Length == before - (new[] { 3, 5, 8, 10 }.Contains(count) ? 1 : 0), "Unequip threshold");
            }
            foreach (var b in Bases(realm))
            {
                var item = GameData.ItemById[GameData.GearId(b, q)];
                Check(item.SetId == EquipmentEffects.Sets[realm].Id, "Stable SetId");
                var effects = item.Effects ?? Array.Empty<ItemEffect>();
                Check(effects.Length == (q == 0 ? 0 : q == 1 ? 1 : q == 2 ? 2 : q == 3 ? 3 : 4), "Effect unlocks");
                if (q > 0) Check(effects.Single(e => e.Id == "hap_linh").Value == GameData.HapLinh[q], "Hap linh multiplied twice");
                if (q >= 2)
                {
                    var baseline = GameData.EffectsFor(b, 2)!.Single(e => e.Grade == 2);
                    var actual = effects.Single(e => e.Grade == 2);
                    var digits = actual.Id == "tinh_tam" ? 0 : 1;
                    Check(actual.Value == Math.Round(baseline.Value * EquipmentEffects.QualityScale[q], digits, MidpointRounding.AwayFromZero), "Quality scaling");
                }
                foreach (var bolt in effects.Where(e => e.Id == "thien_loi")) Check(bolt.Value2 == 60, "Lightning damage scaled twice");
            }
        }
        var mixed = Player(0, 0, 5); mixed.Realm = 5;
        foreach (var b in Bases(1).Skip(5)) mixed.Equipment.Add(new EquippedItem { Slot = b.Slot!, ItemId = GameData.GearId(b, 9) });
        var mix = EquipmentEffects.Calculate(mixed);
        Check(mix.Sets[0].Count == 5 && mix.Sets[1].Count == 5 && mix.SetBonuses["set_pct"] == 6, "5+5 mixed qualities");
        mixed.Equipment.Clear();
        var slots = Bases(0);
        for (int i = 0; i < 10; i++)
        {
            var r = i < 3 ? 0 : i < 6 ? 1 : 2;
            var b = Bases(r).Single(b => b.Slot == slots[i].Slot);
            mixed.Equipment.Add(new EquippedItem { Slot = b.Slot!, ItemId = GameData.GearId(b, i % 10) });
        }
        mix = EquipmentEffects.Calculate(mixed);
        Check(mix.Sets.Take(3).Select(s => s.Count).SequenceEqual(new[] { 3, 3, 4 }) && mix.SetBonuses["set_pct"] == 9, "3+3+4");
        var invalid = Player(0, 9);
        invalid.Equipment.Add(new EquippedItem { Slot = invalid.Equipment[0].Slot, ItemId = invalid.Equipment[0].ItemId });
        Check(EquipmentEffects.Calculate(invalid).Sets[0].Count == 9, "Duplicate slot counted");
        invalid.Equipment.Clear();
        var high = Bases(5)[0];
        invalid.Equipment.Add(new EquippedItem { Slot = high.Slot!, ItemId = GameData.GearId(high, 9) });
        Check(EquipmentEffects.ValidItems(invalid).Length == 0, "Realm restriction");
        var noSet = GameData.GearBases.First(b => b.Id.StartsWith("pb_"));
        invalid.Realm = 1; invalid.Equipment.Clear();
        invalid.Equipment.Add(new EquippedItem { Slot = noSet.Slot!, ItemId = GameData.GearId(noSet, 9) });
        Check(EquipmentEffects.Calculate(invalid).Sets.All(s => s.Count == 0), "Standalone artifact counted as set");
        var full = EquipmentEffects.Calculate(Player(3, 9));
        Check(full.Effects.Single(e => e.Id == "thien_menh").Effective == .60, "Critical bonus cap");
        Check(full.Effects.Single(e => e.Id == "dia_mach").Effective == 15, "Reflect cap");
        Check(full.Effects.Single(e => e.Id == "tho_thuan").Effective == 20, "Shield cap");
        Check(GameService.GearEffects(Player(0, 0)).Qi == 8, "Phàm full set bonus not wired");
        Check(GameService.GearEffects(Player(0, 9)).Qi == 208, "Old red multiplier still active");
        using (var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options))
        {
            db.Database.OpenConnection(); db.Database.EnsureCreated();
            var saved = Player(5, 9);
            saved.LastTickAt = DateTime.UtcNow; saved.ExploreReadyAt = DateTime.UtcNow.AddHours(1);
            var ids = saved.Equipment.Select(e => e.ItemId).OrderBy(x => x).ToArray();
            db.Players.Add(saved); db.SaveChanges(); db.ChangeTracker.Clear();
            var game = new GameService(db);
            var loaded = game.LoadAsync(saved.Id).GetAwaiter().GetResult();
            Check(loaded.Equipment.Select(e => e.ItemId).OrderBy(x => x).SequenceEqual(ids), "Save roundtrip lost gear");
            var state = game.BuildStateAsync(loaded).GetAwaiter().GetResult();
            Check(state.GearEffects!.Sets[5].Count == 10 && state.StatSources!.Length == 6, "State snapshot missing");
            Check(state.CombatLimits!.All(e => e.Effective <= e.Cap), "State total caps");
            Check(state.Stats == GameService.Stats(loaded), "State stats mismatch");
            Check(state.Rate > 0 && state.ExploreQiGain > 0, "Rate integration");
            game.Unequip(loaded, loaded.Equipment.First().Slot);
            var after = game.BuildStateAsync(loaded).GetAwaiter().GetResult();
            Check(after.GearEffects!.Sets[5].Count == 9 && after.GearEffects.Sets[5].Active.SequenceEqual(new[] { 3, 5, 8 }), "Unequip response stale");
            Check(after.Rate < state.Rate && after.Stats.Atk < state.Stats.Atk, "Bonuses not removed");
        }
        var fixtures = new[] { Player(0, 0, 0), Player(0, 2, 3), Player(0, 9), mixed, Player(3, 9) };
        File.WriteAllText("effects-ui.json", System.Text.Json.JsonSerializer.Serialize(new
        {
            equipmentSets = EquipmentEffects.Sets,
            scenarios = fixtures.Select(p => new { snapshot = EquipmentEffects.Calculate(p), items = EquipmentEffects.ValidItems(p) }),
            realms = GameData.Realms.Select(r => new { r.Name })
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
        Console.WriteLine("PASS: 540 milestone cases, unequip, 60 realm/quality effect sets, mixed sets, invalid items, caps and no old rarity multiplier.");
    }

    public static void Report()
    {
        using var output = new StreamWriter("effects-matrix.csv");
        output.WriteLine("Realm,Stage,Quality,Difficulty,Build,Area,Win,Turns,HpLeft,QiBonus,ExploreQiBonus,SkillBonus,QiPerSecond,ExploreQi");
        var rows = 0;
        foreach (var realm in Enumerable.Range(0, 6)) foreach (var stage in new[] { 0, GameData.Realms[realm].Stages.Length - 1 })
        foreach (var q in Enumerable.Range(0, 10)) foreach (var diff in Enumerable.Range(0, 3))
        foreach (var build in new[] { "basic", "skills", "mixed" })
        {
            var p = Player(realm, q); p.Stage = stage; p.Diff = diff;
            p.Chapter = GameData.Chapters.First(c => c.Realm == GameData.Realms[realm].Name).Index;
            p.ChapterMax = p.Chapter;
            if (build == "skills")
            {
                // A reachable starter fire build; avoid granting unearned high-tier passives.
                p.SetSkillLevels(new() { ["hoa_linh_can"] = 1, ["liet_diem"] = 1 });
                p.SkillSlots = "hoa_cau,liet_diem";
            }
            if (build == "mixed" && realm > 0)
                foreach (var equipped in p.Equipment.Skip(5))
                {
                    var b = Bases(realm - 1).Single(b => b.Slot == equipped.Slot);
                    equipped.ItemId = GameData.GearId(b, q);
                }
            foreach (var area in new[] { 1, GameData.BossArea })
            {
                var battle = GameService.BattleAt(p, area, DateTime.UnixEpoch);
                var fx = GameService.GearEffects(p);
                var rate = GameData.QiRate(GameData.Realms[realm], stage) * (1 + (EquipmentEffects.ValidItems(p).Sum(i => i.QiPct) + fx.Qi) / 100);
                var gain = GameData.QiRate(GameData.Realms[realm], stage) * GameData.ExploreQiSeconds * (1 + fx.ExploreQi / 100);
                output.WriteLine(FormattableString.Invariant($"{realm},{stage},{q},{diff},{build},{area},{battle.Win},{GameService.LastTurns},{battle.HeroHpLeft.LastOrDefault()},{fx.Qi},{fx.ExploreQi},{fx.SkillPct},{rate},{gain}"));
                rows++;
            }
        }
        Console.WriteLine($"Wrote {rows} deterministic encounters to effects-matrix.csv");
    }
}
