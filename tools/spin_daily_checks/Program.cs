using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;

const string connection="Data Source=spin_daily_checks;Mode=Memory;Cache=Shared";
using var keeper=new Microsoft.Data.Sqlite.SqliteConnection(connection);keeper.Open();
using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(connection).Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
db.Database.ExecuteSqlRaw("ALTER TABLE Players DROP COLUMN SpinDay; ALTER TABLE Players DROP COLUMN SpinDailyUsed;");
SchemaUpgrade.Apply(db);SchemaUpgrade.Apply(db);
var game=new GameService(db);var checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
void Reject(Player p,Action action,string label){
    var day=p.SpinDay;var used=p.SpinDailyUsed;var inventory=string.Join(';',p.Inventory.OrderBy(x=>x.ItemId).Select(x=>$"{x.ItemId}:{p.Count(x.ItemId)}"));var history=p.SpinHistory;
    var rejected=false;try{action();}catch(GameException){rejected=true;}
    Check(rejected,label);Check(p.SpinDay==day&&p.SpinDailyUsed==used&&p.SpinHistory==history&&string.Join(';',p.Inventory.OrderBy(x=>x.ItemId).Select(x=>$"{x.ItemId}:{p.Count(x.ItemId)}"))==inventory,"failed spin consumes nothing: "+label);
}
var before=DateTimeOffset.Parse("2026-10-21T23:59:59+07:00");var after=before.AddSeconds(1);
var p=await game.CreateAsync("Daily checks");p.Realm=6;p.AutoExplore=false;
p.Add(GameData.SpinTicket,50);p.Add(GameData.PetSpinTicket,50);
p.SpinDay="2026-10-21";p.SpinDailyUsed=89;
game.Spin(p,1,before);Check(p.SpinDailyUsed==90,"single equipment spin uses one daily turn");
game.PetSpin(p,10,before);Check(p.SpinDailyUsed==100,"pet x10 shares the equipment quota and counts ten");
Reject(p,()=>game.Spin(p,1,before),"101st equipment spin blocked");
Reject(p,()=>game.PetSpin(p,1,before),"101st pet spin blocked");
Check(GameService.SpinDailyFor(p,before).Remaining==0,"before midnight remains exhausted");
var next=GameService.SpinDailyFor(p,after);
Check(next.Remaining==100&&next.Used==0,"quota resets exactly at midnight UTC+7");
Check(next.ResetAt==DateTimeOffset.Parse("2026-10-23T00:00:00+07:00").ToUnixTimeMilliseconds(),"next reset is next local midnight");
Check(GameService.SpinDailyFor(p,after.ToUniversalTime()).Remaining==100,"reset is independent of host timezone");
game.PetSpin(p,1,after);Check(p.SpinDay=="2026-10-22"&&p.SpinDailyUsed==1,"first spin records new day");
p.SpinDailyUsed=95;
Reject(p,()=>game.Spin(p,10,after),"x10 rejected when only five remain");
Reject(p,()=>game.PetSpin(p,10,after),"pet x10 rejected when only five remain");
game.Spin(p,5,after);Check(p.SpinDailyUsed==100,"exact remaining batch succeeds");
await db.SaveChangesAsync();var id=p.Id;db.ChangeTracker.Clear();p=await db.Players.Include(x=>x.Inventory).SingleAsync(x=>x.Id==id);
Check(GameService.SpinDailyFor(p,after).Remaining==0,"quota survives save and reload");
Reject(p,()=>game.PetSpin(p,1,after),"reload cannot bypass cap");
var empty=new Player{SpinDay="2026-10-21",SpinDailyUsed=100};
Reject(empty,()=>game.PetSpin(empty,1,after),"no tickets on new day");
Reject(empty,()=>game.Spin(empty,1,after),"no equipment tickets on new day");
var full=new Player{Realm=0,SpinDay="2026-10-21",SpinDailyUsed=100};full.Add(GameData.SpinTicket,2);
full.Add(GameData.GearId(GameData.SpinPool(0)[0],0),GameService.BagCap(full));
Reject(full,()=>game.Spin(full,1,after),"full bag on new day");
var legacy=new Player{SpinHistory=$"{before.ToUnixTimeSeconds()}|a,b,c;{before.AddDays(-1).ToUnixTimeSeconds()}|old;invalid|x"};
Check(GameService.SpinDailyFor(legacy,before).Used==3,"legacy history seeds today's retained turns only");
Check(GameService.SpinDailyFor(legacy,after).Used==0,"legacy history respects midnight");
// Trùng tu không cấp lại lượt quay trong cùng ngày.
p.SpinDay=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");p.SpinDailyUsed=100;
await db.SaveChangesAsync();p=await game.ResetAsync(p);
Check(GameService.SpinDailyFor(p).Remaining==0,"character reset preserves daily cap");
Check((await game.BuildStateAsync(p)).SpinDaily!.Limit==100,"client state exposes daily quota");
Console.WriteLine($"PASS: {checks} daily limit, shared modes, batch, midnight UTC+7, migration, persistence and atomic failure checks.");
