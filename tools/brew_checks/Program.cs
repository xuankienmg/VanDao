using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VanDao.Data;
using VanDao.Game;

using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
var game=new GameService(db);var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
string Snapshot(Player p)=>JsonSerializer.Serialize(p.Inventory.OrderBy(x=>x.ItemId));
void Reject(Player p,Action action,string message){var before=Snapshot(p);var rejected=false;try{action();}catch(GameException){rejected=true;}Check(rejected,message);Check(Snapshot(p)==before,"failure is atomic: "+message);}
Check(GameData.Synths.Count==0,"no synthesis recipes exposed");
Check(GameData.Brews.Select(b=>b.Out).Distinct().Count()==GameData.Brews.Count,"unique output recipe");
Check(GameData.Brews.Single(b=>b.Out=="tu_khi_dan").In["tu_linh_thao"]==2,"Qi pill needs two herbs");
for(var tier=1;tier<GameData.PillChain.Length;tier++){
    var recipe=GameData.Brews.Single(b=>b.Out==GameData.PillChain[tier]);
    Check(recipe.In[GameData.PillHerbs[tier-1]]==2&&recipe.In[GameData.BreakHerbs[tier-1]]==1,"later cultivation pill has two primary herbs and one secondary: "+recipe.Out);
}
foreach(var item in GameData.AllItems.Where(d=>d.Type is "pill" or "elixir"))Check(GameData.Brews.Any(b=>b.Out==item.Id),"every pill and elixir is craftable: "+item.Id);
foreach(var b in GameData.Brews){
    Check(b.In.All(kv=>kv.Value>0&&GameData.ItemById[kv.Key].Type is "herb" or "stone"),"only herbs and stones, positive costs: "+b.Out);
    var p=new Player{Realm=6};foreach(var pill in GameData.PillChain)p.Add(pill,50);
    foreach(var (id,n) in b.In)p.Add(id,n*3);
    var before=p.Count(b.Out);var result=game.Brew(p,b.Out,1);
    Check(p.Count(b.Out)==before+1&&result.craft.Recipe=="brew>"+b.Out,"craft one: "+b.Out);
    foreach(var (id,n) in b.In)Check(p.Count(id)==n*2,"exact one-batch cost: "+b.Out+"/"+id);
    foreach(var pill in GameData.PillChain.Where(id=>id!=b.Out))Check(p.Count(pill)==50,"lower pills untouched: "+b.Out+"/"+pill);
    game.Brew(p,b.Out,0);Check(p.Count(b.Out)==before+3,"craft max: "+b.Out);
    Reject(p,()=>game.Brew(p,b.Out,1),"missing ingredients: "+b.Out);
    var batch=new Player{Realm=6};foreach(var (id,n) in b.In)batch.Add(id,n*11);
    var ten=game.Brew(batch,b.Out,10);
    Check(batch.Count(b.Out)==10&&ten.craft.Output.Qty==10,"exact ten-output batch: "+b.Out);
    foreach(var (id,n) in b.In)Check(batch.Count(id)==n,"exact ten-batch cost: "+b.Out+"/"+id);
    Reject(batch,()=>game.Brew(batch,b.Out,10),"not enough for ten must not craft partial batch: "+b.Out);
    Reject(batch,()=>game.Brew(batch,b.Out,-1),"negative batch rejected: "+b.Out);
}
var legacy=new Player();foreach(var id in GameData.PillChain)legacy.Add(id,99);legacy.Add("linh_thach",1000000);
Reject(legacy,()=>game.Synthesize(legacy,"tu_khi_dan",1),"old endpoint disabled");
Reject(legacy,()=>game.Synthesize(legacy,"luyen_hu_dan",0),"old maximum synthesis disabled");
var locked=new Player{Realm=2};foreach(var (id,n) in GameData.Brews.Single(b=>b.Out=="ngo_dao_dan").In)locked.Add(id,n);
Reject(locked,()=>game.Brew(locked,"ngo_dao_dan",1),"Nguyen Anh gate preserved");
var common=GameData.Brews.Single(b=>b.Out=="tu_khi_dan");var gear=GameData.GearId(GameData.GearBases.First(b=>b.MinRealm==0&&b.Slot!=null),0);
var full=new Player();foreach(var (id,n) in common.In)full.Add(id,n*2);
while(GameService.BagUsed(full)<GameService.BagCap(full))full.Add(gear,1);
Reject(full,()=>game.Brew(full,common.Out,1),"full bag without freeing ingredient stack");
game.Brew(full,common.Out,0);Check(full.Count(common.Out)==2&&GameService.BagUsed(full)<=GameService.BagCap(full),"full bag succeeds when max frees an ingredient stack");
Console.WriteLine($"PASS: {checks} full recipe coverage, herb-only costs, one/max crafting, gates, full bags and retired synthesis checks.");
if(args.Length>0)File.WriteAllText(args[0],JsonSerializer.Serialize(new{brews=GameData.Brews,pillChain=GameData.PillChain,realms=GameData.Realms.Select(r=>new{r.Name}),lockedRealms=GameData.LockedRealms,items=GameData.AllItems.Select(d=>new{d.Id,d.Name,d.Type,d.Rarity,d.MinRealm,d.Tier,d.Use,d.Desc,d.Icon})},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));
