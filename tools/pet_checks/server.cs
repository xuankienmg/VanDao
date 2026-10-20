using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;
using System.Text.Json;

var checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
void Reject(Action action,string label){try{action();}catch(GameException){Check(true,label);return;}throw new Exception("Expected rejection: "+label);}
var file=Path.Combine(Path.GetTempPath(),"vandao-pets-"+Guid.NewGuid()+".db");
try {
using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source="+file).Options);
db.Database.EnsureCreated();
// Simulate an actual pre-pet save, then run the upgrade twice.
db.Database.ExecuteSqlRaw("DROP TABLE Pets; ALTER TABLE Players DROP COLUMN Hatch;");
SchemaUpgrade.Apply(db);SchemaUpgrade.Apply(db);
var game=new GameService(db);var p=await game.CreateAsync("Pet checks");
Check(p.Hatch==""&&p.Pets.Count==0,"legacy schema upgraded idempotently");
Check(PetData.Definitions.Length==12&&PetData.Definitions.All(d=>d.Forms.Length==3),"12 species and 36 forms");
Check(PetData.Items.All(i=>!GameService.Tradable(i,true)),"all pet items remain nontradable with admin ticket allowance");
p.Add(PetData.EggIds[4],3);game.Hatch(p,PetData.EggIds[4]);
Reject(()=>game.Hatch(p,PetData.EggIds[4]),"occupied hatch rejects");
Reject(()=>game.CollectHatch(p),"early collection rejects");
Reject(()=>game.Hatch(p,PetData.EggIds[4],1),"second hatch requires realm 2");
p.Realm=2;game.UseItem(p,PetData.EggIds[4]);
Check(GameService.PetStateFor(p).Hatch.Any(h=>h.Slot==1),"inventory egg use selects next free hatch slot");
Reject(()=>game.UseItem(p,PetData.EggIds[4]),"inventory egg use rejects full incubator");
p.Hatch=string.Join(';',p.Hatch.Split(';').Select(s=>s[..(s.LastIndexOf('|')+1)]+DateTimeOffset.UtcNow.AddHours(-9).ToUnixTimeSeconds()));
await db.SaveChangesAsync();db.ChangeTracker.Clear();p=await game.LoadAsync(p.Id);
game.CollectHatch(p);game.CollectHatch(p,1);
await db.SaveChangesAsync();db.ChangeTracker.Clear();p=await game.LoadAsync(p.Id);
Check(p.Pets.Count==2&&p.Pets.All(x=>x.AptAtk>=80&&x.AptHp<=100),"offline ready timestamps persist and legendary aptitude bounded");
Reject(()=>game.CollectHatch(p),"double collection rejects");
var pet=p.Pets[0];game.SetPetSlot(p,pet.Id,1);game.SetPetSlot(p,p.Pets[1].Id,1);
Check(pet.Slot==0&&p.Pets[1].Slot==1,"active slot unique replacement");
Reject(()=>game.SetPetSlot(p,pet.Id,3),"second guard requires realm 4");
game.SetPetSlot(p,pet.Id,2);Check(GameService.PetBonus(p).Hp>0,"guard and active add stats");
p.Add("linh_thu_dan",100000);p.Realm=0;pet.Level=1;game.Feed(p,pet.Id,0);
Check(pet.Level==20&&p.Count("linh_thu_dan")>0,"max feed stops at realm cap");
Reject(()=>game.Feed(p,pet.Id,1),"feed at cap consumes nothing");
p.Realm=2;pet.Level=30;Reject(()=>game.Evolve(p,pet.Id),"evolution rejects missing materials");
p.Add("kim_o_tinh_hach",2);p.Add("huyet_giao_hach",3);p.Add("hoa_hinh_dan",100);p.Add("than_thu_huyet",100);p.Add("linh_thach",1000000);
game.Evolve(p,pet.Id);Check(pet.Form==1,"level 30 evolution");
pet.Level=60;Reject(()=>game.Evolve(p,pet.Id),"final evolution realm 3 gate");p.Realm=3;game.Evolve(p,pet.Id);Check(pet.Form==2,"level 60 evolution");
var consumed=pet.FoodSpent;var food=p.Count("linh_thu_dan");game.Release(p,pet.Id);Check(p.Count("linh_thu_dan")==food+consumed/2,"release refunds floor of half actual food");
Reject(()=>game.Release(p,pet.Id),"double release rejects");
var stranger=new Player();Reject(()=>game.SetPetSlot(stranger,p.Pets[0].Id,1),"foreign ownership rejected");
p.Add(PetData.EggIds[0],1);game.Hatch(p,PetData.EggIds[0]);
var stones=p.Count("linh_thach");var quote=GameService.PetStateFor(p).Hatch.Single().InstantCost;game.HatchNow(p);
Check(stones-p.Count("linh_thach")==quote,"instant hatch charges quoted stones");game.CollectHatch(p);
var baby=p.Pets.Last();var count=p.Count("linh_thu_dan");game.Feed(p,baby.Id,1);Check(p.Count("linh_thu_dan")==count-1,"feed one exact consumption");
count=p.Count("linh_thu_dan");game.Feed(p,baby.Id,10);Check(p.Count("linh_thu_dan")==count-10,"feed ten exact consumption");
Reject(()=>game.Feed(p,baby.Id,-1),"negative feed rejected");
var odd=GameService.ChestOdds(p).Single(x=>x.Chest==GameData.BossChest);
Check(Math.Abs(odd.Pets!.Where(x=>PetData.EggIds.Contains(x.Item)).Sum(x=>x.Pct)-PetData.PetEggDrops[p.Chapter]*100)<1e-8,"displayed egg odds match chapter reward table");
Check(!ChestRewards.Table(0,GameData.BossChest).Any(x=>x.Item=="than_thu_huyet"),"blood drops restricted to hard and nightmare");
Check(ChestRewards.Table(0,GameData.BossChest,2,0).Where(x=>PetData.EggIds.Contains(x.Item)).All(x=>x.Item==PetData.EggIds[2]),"nightmare shifts egg quality one tier");
p.Add(GameData.SpinTicket,10);var spin=game.PetSpin(p,10);Check(spin.craft.Outputs!.Sum(x=>x.Qty)==10&&p.SpinHistory.Contains("thu_dan_"),"pet spin consumes tickets and shares history");
for(var r=0;r<GameData.Realms.Count;r++)foreach(var chest in GameData.Chests.Keys)for(var diff=0;diff<3;diff++)Check(Math.Abs(ChestRewards.Table(r,chest,diff).Sum(x=>x.Probability)-1)<1e-8,$"normalized chest {r}/{chest}/{diff}");
for(var realm=0;realm<GameData.Realms.Count;realm++)foreach(var entry in GameData.Chests)foreach(var difficulty in new[]{0,1,2})foreach(var chapter in new[]{0,3,9})
{
    var rewards=ChestRewards.Table(realm,entry.Key,difficulty,chapter);
    var expected=realm>=GameData.ItemById["ngo_dao_dan"].MinRealm?entry.Value.Enlightenment:0;
    var petProbability=entry.Key==GameData.BossChest?PetData.PetEggDrops[chapter]+.045:.05;
    var scale=(1-expected-petProbability)/(1-expected);
    Check(Math.Abs(rewards.Where(x=>x.Group=="enlightenment").Sum(x=>x.Probability)-expected)<1e-12,$"exact gated enlightenment {realm}/{entry.Key}/{difficulty}/{chapter}");
    Check(Math.Abs(rewards.Sum(x=>x.Probability)-1)<1e-12&&rewards.All(x=>x.Probability>=0),$"normalized pet distribution {realm}/{entry.Key}/{difficulty}/{chapter}");
    Check(Math.Abs(rewards.Where(x=>x.Group=="gear").Sum(x=>x.Probability)-entry.Value.EquipmentChance*scale)<1e-12&&Math.Abs(rewards.Where(x=>x.Group=="herb").Sum(x=>x.Probability)-entry.Value.Herb*scale)<1e-12,$"other branches scaled proportionally {realm}/{entry.Key}/{difficulty}/{chapter}");
}
p.Realm=0;p.Stage=0;p.Chapter=0;foreach(var owned in p.Pets.ToList())game.Release(p,owned.Id);await db.SaveChangesAsync();var baseline=GameService.BattleAt(p,15);
var fighter=new Pet{PlayerId=p.Id,DefId="hoa_linh_ho",Slot=1,Level=1};p.Pets.Add(fighter);db.Pets.Add(fighter);
var b1=GameService.BattleAt(p,15);var b2=GameService.BattleAt(p,15);
Check(JsonSerializer.Serialize(b1)==JsonSerializer.Serialize(b2),"battle including pet casts deterministic");
Check(b1.Pet?.Casts.Length>0,"pet casts produced in long battle");
Check(b1.Pet!.Casts.All(c=>c.Turn>=0&&c.Turn<b1.Hits&&c.Dmg>0),"sampled casts valid damage events");
fighter.Slot=0;
Check(JsonSerializer.Serialize(GameService.BattleAt(p,15))==JsonSerializer.Serialize(baseline),"resting pet leaves all production combat RNG and outcomes unchanged");
fighter.Slot=1;

p.BodyHp=5000;
foreach(var def in PetData.Definitions){
    fighter.DefId=def.Id;var castBattle=GameService.BattleAt(p,15);
    Check(castBattle.Pet!.Casts.Length>0&&castBattle.Pet.Casts.All(c=>c.Turn>=0&&c.Turn<castBattle.Hits),"valid cast timeline "+def.Id);
    Check(JsonSerializer.Serialize(castBattle)==JsonSerializer.Serialize(GameService.BattleAt(p,15)),"repeatable cast "+def.Id);
}
fighter.DefId="bich_thuy_giao";Check(GameService.BattleAt(p,15).Pet!.Casts.Any(c=>c.Heal>0),"healing cast restores actual health");
fighter.DefId="bang_tinh_tho";Check(GameService.BattleAt(p,15).Pet!.Casts.Any(c=>c.Guard>0),"shield cast grants guard");
fighter.DefId="hoa_linh_ho";
await db.SaveChangesAsync();db.ChangeTracker.Clear();p=await game.LoadAsync(p.Id);Check(p.Pets.Count==1,"owned pet persistence");
p=await game.ResetAsync(p);Check(p.Pets.Count==0&&p.Hatch==""&&!db.Pets.Any(),"reset cascades pets and hatch");
var golden=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"no-pet-golden.json")))!;
foreach(var fixture in NoPetRegression.Cases())
    Check(golden[fixture.Name]==NoPetRegression.Hash(GameService.BattleAt(fixture.Player,fixture.Area)),"original production RNG "+fixture.Name);
PetBalance.Print(Check);
Console.WriteLine($"ALL {checks} CHECKS PASSED");
}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(file);}

