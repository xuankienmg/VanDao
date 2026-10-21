using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VanDao.Data;
using VanDao.Game;

using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
var game=new GameService(db);var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
string Snapshot(Player p)=>JsonSerializer.Serialize(p.Inventory.OrderBy(x=>x.ItemId));
void Reject(Player p,string items,string message){var before=Snapshot(p);var rejected=false;try{game.Fuse(p,items);}catch(GameException){rejected=true;}Check(rejected,message);Check(Snapshot(p)==before,"rejection consumes nothing: "+message);}
var gear=GameData.GearBases.First(x=>x.Slot!=null&&x.MinRealm==0);
var other=GameData.GearBases.First(x=>x.Slot!=null&&x.MinRealm==1);
string Inputs(string id,int n=9)=>string.Join(',',Enumerable.Repeat(id,n));
Check(GameData.FuseChance.Length==GameData.RarityOrder.Length-1,"all nine transitions configured");
var previous=1d;
for(var q=0;q<GameData.FuseChance.Length;q++){
    var chance=GameData.FuseChance[q];Check(chance>0&&chance<=previous,"valid decreasing rate "+q);previous=chance;
    var id=GameData.GearId(gear,q);var next=GameData.GearId(gear,q+1);
    foreach(var (roll,success) in new[]{(Math.BitDecrement(chance),true),(chance,false)}){
        if(roll>=1)continue;
        var p=new Player();p.Add(id,9);
        var result=game.Fuse(p,Inputs(id),new FixedRandom(roll));
        Check(result.craft.Recipe==(success?"fuse:ok":"fuse:fail"),"exact probability boundary "+q);
        Check(result.craft.Output.Item==(success?next:id),"correct output rarity and base "+q);
        Check(p.Count(id)==(success?0:1)&&p.Count(next)==(success?1:0),"nine inputs consumed, exactly one output "+q);
    }
    var mixed=new Player();var highRealm=GameData.GearId(other,q);mixed.Add(id,8);mixed.Add(highRealm,1);
    var mix=string.Join(',',Enumerable.Repeat(id,8).Append(highRealm));
    var selected=game.Fuse(mixed,mix,new FixedRandom(0,8));
    Check(selected.craft.Output.Item==GameData.GearId(other,q+1),"output follows selected input realm and slot "+q);
    var expectedOutputValue=chance*GameData.SellPrice(GameData.ItemById[next])+(1-chance)*GameData.SellPrice(GameData.ItemById[id]);
    Check(expectedOutputValue<9d*GameData.SellPrice(GameData.ItemById[id]),"fusion cannot generate sell-price arbitrage "+q);
    Console.WriteLine($"{GameData.QualityName[q]} → {GameData.QualityName[q+1]}: {chance:P0}; mean input consumption {1+8/chance:F2}");
}
var invalid=new Player();var common=GameData.GearId(gear,0);invalid.Add(common,9);
Reject(invalid,Inputs(common,8),"eight items");Reject(invalid,Inputs(common,10),"ten items");
Reject(invalid,Inputs(GameData.GearId(gear,1)),"unowned gear");
Reject(invalid,Inputs(common,8)+","+GameData.GearId(gear,1),"mixed quality");
Reject(invalid,Inputs("linh_thach"),"non-equipment");
var max=GameData.GearId(gear,9);invalid.Add(max,9);Reject(invalid,Inputs(max),"highest rarity cannot fuse");
invalid.Add(common,-1);Reject(invalid,Inputs(common),"duplicate ids require enough copies");
Console.WriteLine($"PASS: {checks} probability boundary, inventory, mixed realm, failure and economic checks.");
if(args.Length>0)File.WriteAllText(args[0],JsonSerializer.Serialize(new{fuseChance=GameData.FuseChance,items=Enumerable.Range(0,10).Select(q=>GameData.ItemById[GameData.GearId(gear,q)]).Select(d=>new{d.Id,d.Name,d.Rarity,d.Slot,d.Base,d.ItemLevel,sell=GameData.SellPrice(d)})},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));

sealed class FixedRandom(double roll,int index=0):Random{
    public override double NextDouble()=>roll;
    public override int Next(int maxValue)=>Math.Min(index,maxValue-1);
}
