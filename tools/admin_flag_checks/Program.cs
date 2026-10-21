using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;
using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
var game=new GameService(db);var p=await game.CreateAsync("Admin checks");
var count=0;
void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
p.Flagged=true;
Check(SaveGuard.DataFlagged(p,true),"normal dev player remains flagged");
p.Tainted=true;
Check(!SaveGuard.DataFlagged(p,true),"dev admin exempt");
Check(SaveGuard.DataFlagged(p,false),"production still flagged");
Player.AdminEnabled=false;
var rejected=false;try{await game.AdminClearDataFlagAsync(p);}catch(GameException){rejected=true;}
Check(rejected&&p.Flagged,"non-admin cannot clear flag");
Player.AdminEnabled=true;
await game.AdminClearDataFlagAsync(p);
Check(!p.Flagged&&p.Tainted,"admin clears only data warning");
await db.SaveChangesAsync();var id=p.Id;db.ChangeTracker.Clear();
p=await db.Players.SingleAsync(x=>x.Id==id);
Check(!p.Flagged&&p.Tainted,"clear persists without clearing admin history");
p.Flagged=true;p.Tainted=false;p.Admin="god";
Check(!SaveGuard.DataFlagged(p,true)&&SaveGuard.DataFlagged(p,false),"admin flags exempt only dev");
p.Admin="";p.AdminPoints=1;
Check(!SaveGuard.DataFlagged(p,true),"admin points exempt dev");
Player.AdminEnabled=false;
rejected=false;try{await game.AdminClearDataFlagAsync(p,Guid.NewGuid().ToString());}catch(GameException){rejected=true;}
Check(rejected,"non-admin cannot clear other player flag");
Player.AdminEnabled=true;p.Flagged=true;
rejected=false;try{await game.AdminClearDataFlagAsync(p,"invalid-id");}catch(GameException){rejected=true;}
Check(rejected&&p.Flagged,"invalid target cannot clear actor flag");
rejected=false;try{await game.AdminClearDataFlagAsync(p,Guid.NewGuid().ToString());}catch(GameException){rejected=true;}
Check(rejected&&p.Flagged,"missing online service cannot clear unrelated player or actor");
Console.WriteLine($"PASS: {count} admin flag authorization, exemption and persistence checks.");
