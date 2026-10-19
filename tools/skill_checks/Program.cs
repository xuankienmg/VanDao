using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VanDao.Data;
using VanDao.Game;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
static void Reject(Action action, string reason)
{
    try { action(); } catch (GameException e) { Check(e.Message.Contains(reason), e.Message); return; }
    throw new Exception("Expected rejection: " + reason);
}
using var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
var game = new GameService(db);
foreach (var element in SkillData.Elements.Where(e => e.Open))
{
    var actives = SkillData.Skills.Where(s => s.Element == element.Id && s.Tree == "active").ToArray();
    foreach (var realm in Enumerable.Range(0, GameData.Realms.Count))
        Check(actives.Count(s => s.MinRealm == realm) == 1, $"{element.Name}: expected one active skill in realm {realm}");
    foreach (var skill in actives)
    {
        var learner = new Player { Realm = skill.MinRealm - 1, AdminPoints = 1000 };
        learner.SetSkillLevels(skill.Req.ToDictionary(r => r.Split(':')[0], r => int.Parse(r.Split(':')[1])));
        Reject(() => game.UpgradeSkill(learner, skill.Id), "cảnh giới");
        learner.Realm = skill.MinRealm;
        game.UpgradeSkill(learner, skill.Id);
        Check(GameService.SkillLevels(learner)[skill.Id] == 1, "Cannot learn active at assigned realm");
    }
}
foreach (var skill in SkillData.Skills.Where(s => SkillData.Elements.Any(e => e.Id == s.Element && e.Open)))
    foreach (var req in skill.Req)
        Check(SkillData.ById[req.Split(':')[0]].MinRealm <= skill.MinRealm, "Prerequisite requires a higher realm: " + skill.Id);
Check(SkillData.ById["hoa_long"].MinRealm == 3, "Fire dragon must unlock at Nguyen Anh");
var fresh = SkillData.Skills.Where(s => s.MinRealm >= 4).ToArray();
Check(fresh.Length == 8, "Eight new skills expected");
foreach (var s in fresh)
{
    var p = new Player { Realm = s.MinRealm - 1, AdminPoints = 1000 };
    var levels = s.Req.ToDictionary(r => r.Split(':')[0], r => int.Parse(r.Split(':')[1]));
    levels["hoa_linh_can"] = 1;
    p.SetSkillLevels(levels);
    Reject(() => game.UpgradeSkill(p, s.Id), "cảnh giới");
    p.Realm = s.MinRealm;
    var firstReq = s.Req[0].Split(':')[0];
    var before = levels[firstReq]; levels[firstReq] = 0; p.SetSkillLevels(levels);
    Reject(() => game.UpgradeSkill(p, s.Id), SkillData.ById[firstReq].Name);
    levels[firstReq] = before; p.SetSkillLevels(levels);
    p.AdminPoints = -10000;
    Reject(() => game.UpgradeSkill(p, s.Id), "điểm kỹ năng");
    p.AdminPoints = 1000;
    var points = GameService.SkillPoints(p);
    var stats = GameService.Stats(p);
    game.UpgradeSkill(p, s.Id);
    Check(GameService.SkillLevels(p)[s.Id] == 1, "Skill was not learned");
    Check(GameService.SkillPoints(p) == points - s.Cost, "Wrong learning cost");
    Check(GameService.SkillLevels(p)[firstReq] == before, "Existing progress changed");
    if (s.HpPct > 0) Check(GameService.Stats(p).Hp > stats.Hp, "Passive HP bonus missing");
    if (s.AtkPct > 0) Check(GameService.Stats(p).Atk > stats.Atk, "Passive attack bonus missing");
    if (s.DefPct > 0) Check(GameService.Stats(p).Def > stats.Def, "Passive defense bonus missing");
    if (s.Tree == "active")
    {
        game.SetSkillSlot(p, 1, s.Id);
        Check(p.Slots()[1] == s.Id && s.DmgBase > 0, "Cannot equip new active skill");
    }
    game.UpgradeSkill(p, s.Id);
    Check(GameService.SkillLevels(p)[s.Id] == 2, "Cannot upgrade new skill");
}
foreach (var s in SkillData.Skills)
    foreach (var r in s.Req)
        Check(SkillData.ById.ContainsKey(r.Split(':')[0]), "Missing prerequisite");
if (args.Length > 0)
    File.WriteAllText(args[0], JsonSerializer.Serialize(new { realms = GameData.Realms.Select(r => new { r.Name }), elements = SkillData.Elements, skillTree = SkillData.Skills }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
Console.WriteLine("PASS: eight skills enforce realms, prerequisites and points; learning/upgrading/equipping preserves prior progress.");

var alchemist = new Player { Realm = 2 };
var recipe = GameData.Brews.Single(b => b.Out == "ngo_dao_dan");
foreach (var (id, count) in recipe.In) alchemist.Add(id, count * 2);
Reject(() => game.Brew(alchemist, recipe.Out, 1), "Nguyên Anh");
Check(alchemist.Count("thien_nien_sam") == 10, "Locked brew consumed ingredients");
alchemist.Realm = 3;
game.Brew(alchemist, recipe.Out, 0);
Check(alchemist.Count(recipe.Out) == 2 && recipe.In.All(kv => alchemist.Count(kv.Key) == 0), "Wrong batch yield or ingredient cost");
alchemist.Realm = 2;
Reject(() => game.Use(alchemist, recipe.Out), "Nguyên Anh");
Check(alchemist.Count(recipe.Out) == 2 && alchemist.ElixirPoints == 0, "Locked use consumed pill or granted points");
alchemist.Realm = 3;
var initialPoints = GameService.SkillPoints(alchemist);
game.Use(alchemist, recipe.Out);
game.Use(alchemist, recipe.Out);
Check(GameService.SkillPoints(alchemist) == initialPoints + 2 && alchemist.ElixirPoints == 2 && alchemist.Count(recipe.Out) == 0, "Each pill must grant exactly one point");
Reject(() => game.Use(alchemist, recipe.Out), "Không có");
alchemist.SetSkillLevels(new() { ["hoa_linh_can"] = 2 });
alchemist.Add("tay_tuy_dan", 1);
game.Use(alchemist, "tay_tuy_dan");
Check(alchemist.ElixirPoints == 2, "Skill reset lost pill points");
db.Database.OpenConnection();
db.Database.EnsureCreated();
db.ChangeTracker.Clear();
db.Players.Add(alchemist);
db.SaveChanges();
db.ChangeTracker.Clear();
Check(db.Players.Single(p => p.Id == alchemist.Id).ElixirPoints == 2, "Pill points did not persist");
Console.WriteLine("PASS: enlightenment pill realm gates, batch costs, +1 per use, skill reset and database persistence.");

var fighter = new Player { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), BodyHp = 100000 };
var sawCrit = false; var sawNormal = false;
for (var trial = 0; trial < 100; trial++)
{
    var trip = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc).AddSeconds(trial);
    var battle = GameService.BattleAt(fighter, 1, trip);
    Check(JsonSerializer.Serialize(battle) == JsonSerializer.Serialize(GameService.BattleAt(fighter, 1, trip)), "Battle replay changed crit outcomes");
    Check(battle.BasicDamage!.Length == battle.Hits && battle.BasicCritical!.Length == battle.Hits, "Sampled damage arrays do not match turns");
    for (var turn = 0; turn < battle.Hits; turn++)
    {
        var wave = battle.Waves!.Last(w => w.Start <= turn);
        var critical = battle.BasicCritical![turn];
        sawCrit |= critical; sawNormal |= !critical;
        var raw = GameService.Stats(fighter).Atk * 115.0 / 100;
        var reduction = wave.Tier == "elite" ? 1 - GameData.EliteDmgReduce / 100 : 1;
        var expected = Math.Max(1, (int)Math.Round(Math.Max(1, (int)Math.Round(raw * (critical ? 1.5 : 1)) - wave.Def) * reduction));
        Check(battle.BasicDamage[turn] == expected, "Crit damage does not match attack, defense and damage reduction");
    }
}
Check(sawCrit && sawNormal, "Expected both critical and normal hits across seeded battles");
fighter.SkillSlots = "hoa_cau,liet_diem";
fighter.SetSkillLevels(new() { ["liet_diem"] = 1 });
fighter.Chapter = 2;
var sawSkillCrit = false; var sawSkillNormal = false;
for (var trial = 0; trial < 100; trial++)
{
    var battle = GameService.BattleAt(fighter, 1, new DateTime(2026, 10, 19).AddSeconds(trial));
    foreach (var cast in battle.Casts.Where(c => c.Dmg > 0))
    {
        sawSkillCrit |= cast.Critical; sawSkillNormal |= !cast.Critical;
        Check(battle.BasicDamage![cast.Turn] == 0 && !battle.BasicCritical![cast.Turn], "Active skill also dealt a basic hit");
        var wave = battle.Waves!.Last(w => w.Start <= cast.Turn);
        var raw = GameService.Stats(fighter).Atk * SkillData.Damage(SkillData.ById[cast.Skill], 1) / 100;
        var reduction = wave.Tier == "elite" ? 1 - GameData.EliteDmgReduce / 100 : 1;
        var expected = Math.Max(1, (int)Math.Round(Math.Max(1, (int)Math.Round(raw * (cast.Critical ? 1.5 : 1)) - wave.Def) * reduction));
        Check(cast.Dmg == expected, "Active skill critical damage formula mismatch");
    }
}
Check(sawSkillCrit && sawSkillNormal, "Expected both critical and normal active skill casts");
Console.WriteLine("PASS: critical damage formula, per-turn flags, sampled arrays and deterministic replay.");
