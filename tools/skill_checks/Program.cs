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
