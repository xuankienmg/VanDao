using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;

const string connection="Data Source=manual-checks;Mode=Memory;Cache=Shared";
using var keeper=new Microsoft.Data.Sqlite.SqliteConnection(connection);keeper.Open();
using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(connection).Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
var game=new GameService(db);var count=0;
void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
void Reject(Action action,string message){try{action();}catch(GameException){count++;return;}throw new Exception(message);}
foreach (var ticket in new[] { GameData.SpinTicket, GameData.PetSpinTicket }) {
    Check(GameService.Tradable(GameData.ItemById[ticket], true), "admin cannot trade ticket: " + ticket);
    Check(!GameService.Tradable(GameData.ItemById[ticket]), "normal player can list ticket: " + ticket);
}
foreach (var admin in new[] { new Player { Admin = "god" }, new Player { AdminPoints = 1 }, new Player { Tainted = true } }) {
    Check(GameService.CanSellSpinTickets(admin, true), "development admin cannot list tickets");
    Check(!GameService.CanSellSpinTickets(admin, false), "admin can list tickets outside development");
}
Check(!GameService.CanSellSpinTickets(new Player(), true), "ordinary development player can list tickets");
Check(!GameService.CanSellSpinTickets(new Player(), false), "ordinary player can list tickets");
var qiPills = new[] { "bo_nguyen_dan_ha", "bo_nguyen_dan_trung", "bo_nguyen_dan_thuong" };
for (var tier = 0; tier < qiPills.Length; tier++) {
    var pillId = qiPills[tier];
    Check(GameData.ItemById.ContainsKey(pillId), "missing qi pill: " + pillId);
    var pill = GameData.ItemById[pillId];
    Check(pill.UseQiSeconds == new[] { 300, 1800, 7200 }[tier], "wrong qi pill potency");
    var recipe = GameData.Brews.Single(b => b.Out == pillId);
    var brewer = await game.CreateAsync("Qi pill " + tier);
    brewer.Realm = 2;
    foreach (var (item, qty) in recipe.In) brewer.Add(item, qty * 2);
    var beforeBrew = recipe.In.ToDictionary(kv => kv.Key, kv => brewer.Count(kv.Key));
    game.Brew(brewer, pillId, 2);
    Check(brewer.Count(pillId) == 2 && recipe.In.All(kv => brewer.Count(kv.Key) == beforeBrew[kv.Key] - kv.Value * 2), "brew did not consume exact materials");
    brewer.Qi = 0;
    var stateBefore = await game.BuildStateAsync(brewer);
    game.UseItem(brewer, pillId);
    Check(Math.Abs(brewer.Qi - Math.Min(stateBefore.QiReq, stateBefore.Rate * pill.UseQiSeconds)) < .001, "incorrect qi gain");
    Check(brewer.Count(pillId) == 1, "qi pill not consumed");
    brewer.Qi = stateBefore.QiReq - 1;
    game.UseItem(brewer, pillId);
    Check(brewer.Qi == stateBefore.QiReq, "qi pill overflowed cultivation cap");
}
var p=await game.CreateAsync("Manual checks");
Check(p.Count(CultivationManuals.Starter)==1&&CultivationManuals.Unlocked(p).Length==0,"new player starts with unread fire manual");
Reject(()=>game.UpgradeSkill(p,"liet_diem"),"locked tree learned without book");
Reject(()=>game.SetSkillSlot(p,0,"thuy_cau"),"locked free basic equipped");
Reject(()=>game.UseItem(p,"bi_kip_thuy"),"missing book learned");
Reject(()=>game.UpgradeSkill(p,"thuong_hai"),"locked higher skill learned without book");
game.UseItem(p,CultivationManuals.Starter);
Check(CultivationManuals.IsUnlocked(p,"hoa")&&p.Count(CultivationManuals.Starter)==0,"fire manual unlocks and consumes one");
p.Add(CultivationManuals.Starter,2);
Reject(()=>game.UseItem(p,CultivationManuals.Starter),"duplicate manual accepted");
Check(p.Count(CultivationManuals.Starter)==2,"duplicate manual was consumed");
p.AdminPoints=1000;p.Realm=2;
foreach(var m in CultivationManuals.Available){
    if(m.Element!="hoa"){p.Add(m.Item,1);game.UseItem(p,m.Item);}
    var root=SkillData.Skills.First(s=>s.Element==m.Element&&s.Category=="Căn cơ");
    if(GameService.SkillLevels(p).GetValueOrDefault(root.Id)==0)game.UpgradeSkill(p,root.Id);
    Check(GameService.SkillLevels(p).GetValueOrDefault(root.Id)>0,"root unavailable after manual: "+m.Element);
}
game.UpgradeSkill(p,"liet_diem");p.Add("tay_tuy_dan",1);game.UseItem(p,"tay_tuy_dan");
Check(CultivationManuals.Unlocked(p).Length==3,"respec forgot manuals");
var id=p.Id;await db.SaveChangesAsync();db.ChangeTracker.Clear();
p=await db.Players.Include(x=>x.Inventory).SingleAsync(x=>x.Id==id);
Check(CultivationManuals.Unlocked(p).Length==3&&p.ManualVersion==1,"unlocks lost after save/reload");
var state=await game.BuildStateAsync(p);
Check(state.UnlockedElements?.Length==3,"unlocks absent from state");
foreach(var manual in CultivationManuals.All.Except(CultivationManuals.Available)){
    p.Add(manual.Item,1);
    Reject(()=>game.UseItem(p,manual.Item),"unavailable manual learned");
    Check(p.Count(manual.Item)==1,"unavailable manual consumed");
    CultivationManuals.Unlock(p,manual.Element);
    Check(!CultivationManuals.IsUnlocked(p,manual.Element),"disabled tree exposed by saved unlock");
    var root=SkillData.Skills.First(s=>s.Element==manual.Element&&s.Category=="Căn cơ");
    Reject(()=>game.UpgradeSkill(p,root.Id),"disabled root learned");
    p.SetSkillLevels(new Dictionary<string,int>{{root.Id,5}});
    Check(!GameService.SkillLevels(p).ContainsKey(root.Id),"disabled passive still contributes");
}
await db.SaveChangesAsync();
game.AdminAction(p,"resetall",null,1);
Check(p.ManualVersion==1&&p.LearnedManuals==""&&p.Count(CultivationManuals.Starter)==1,"reset did not restore unread starter manual");
await db.SaveChangesAsync();
var old=new Player{Skills="thuy_linh_can:2;moc_linh_can:1",SkillSlots="thuy_cau,thuy_nguyet"};
CultivationManuals.Migrate(old);CultivationManuals.Migrate(old);
Check(CultivationManuals.Unlocked(old).Order().SequenceEqual(new[]{"hoa","moc","thuy"}),"legacy migration not idempotent or loses learned trees");
var fresh=new Player{ManualVersion=1};CultivationManuals.Migrate(fresh);
Check(CultivationManuals.Unlocked(fresh).Length==0,"new player bypassed manual gate");
for(int realm=0;realm<GameData.Realms.Count;realm++)for(int diff=0;diff<3;diff++)for(int chapter=0;chapter<10;chapter++)foreach(var chest in GameData.Chests.Keys){
    var table=ChestRewards.Table(realm,chest,diff,chapter);
    Check(Math.Abs(table.Sum(x=>x.Probability)-1)<1e-8,"chest total not one");
    var manuals=table.Where(x=>x.Group=="manual").ToArray();
    Check(Math.Abs(manuals.Sum(x=>x.Probability)-(chest==GameData.BossChest?.01:0))<1e-10,"wrong manual chance");
    Check(manuals.All(x=>x.Qty==1&&GameData.ItemById[x.Item].Type=="manual"),"invalid manual reward");
    if(chest==GameData.BossChest){
        Check(manuals.Length==3&&manuals.All(x=>Math.Abs(x.Probability-.01/3)<1e-10),"manuals not equally likely");
        var cumulative=0.0;
        foreach(var reward in table){
            if(reward.Group=="manual")Check(ChestRewards.Roll(realm,chest,new FixedRandom(cumulative+reward.Probability/2),diff,chapter).Item==reward.Item,"manual cannot be rolled");
            cumulative+=reward.Probability;
        }
    }
}
db.Database.ExecuteSqlRaw("ALTER TABLE Players DROP COLUMN LearnedManuals");
db.Database.ExecuteSqlRaw("ALTER TABLE Players DROP COLUMN ManualVersion");
SchemaUpgrade.Apply(db);SchemaUpgrade.Apply(db);
db.Database.OpenConnection();
using(var cmd=db.Database.GetDbConnection().CreateCommand()){
    cmd.CommandText="SELECT ManualVersion FROM Players LIMIT 1";
    Check(Convert.ToInt32(cmd.ExecuteScalar())==0,"legacy schema default version incorrect");
    cmd.CommandText="SELECT LearnedManuals FROM Players LIMIT 1";
    Check(Convert.ToString(cmd.ExecuteScalar())=="","legacy schema default unlocks incorrect");
}
Console.WriteLine($"PASS {count} manual authorization, consumption, respec, reset, persistence, legacy/schema migration and chest distribution checks.");
class FixedRandom(double value):Random{public override double NextDouble()=>value;}
