using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;

using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
var game=new GameService(db);var p=await game.CreateAsync("Map preview checks");
p.Chapter=1;p.ChapterMax=1;p.Area=4;p.AreaMax=4;
p.ExploreReadyAt=DateTime.UtcNow.AddHours(1);await db.SaveChangesAsync();
var ready=p.ExploreReadyAt;var logCount=p.Logs.Count;var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);checks++;}
void Reject(Action action,string message){try{action();}catch(GameException){Check(true,message);return;}throw new Exception(message);}
var previous=game.PreviewMap(p,0);
Check(previous.Realm==0&&previous.Area==0&&previous.AreaMax==15,"viewing chapter 1 does not mark 1-15 as current");
Check(p.Chapter==1&&p.Area==4&&p.ExploreReadyAt==ready&&p.Logs.Count==logCount,"preview preserves chapter 2-4, trip timer and logs");
var current=game.PreviewMap(p,1);
Check(current.Realm==1&&current.Area==4,"returning to current chapter marks 2-4");
Reject(()=>game.PreviewMap(p,2),"locked chapter preview rejected");
Reject(()=>game.SelectMapArea(p,1,5),"unopened area rejected");
Reject(()=>game.SelectMapArea(p,0,GameData.BossArea),"boss without key rejected before travel");
Check(p.Chapter==1&&p.Area==4&&p.ExploreReadyAt==ready,"failed travel preserves active position and timer");
await db.SaveChangesAsync();db.ChangeTracker.Clear();
var fetched=await game.PreviewMapAsync(p.Id,0);
var stored=await db.Players.AsNoTracking().SingleAsync(x=>x.Id==p.Id);
Check(fetched.Area==0&&stored.Chapter==1&&stored.Area==4&&stored.ExploreReadyAt==ready,"preview endpoint path leaves persisted position unchanged");
p=await game.LoadAsync(p.Id);game.SelectMapArea(p,0,3);await db.SaveChangesAsync();
Check(p.Chapter==0&&p.Area==3,"explicit area click travels to exactly 1-3");
Console.WriteLine($"ALL {checks} MAP CHECKS PASSED");
