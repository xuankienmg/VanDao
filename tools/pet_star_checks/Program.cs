using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VanDao.Data;
using VanDao.Game;

using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
var game=new GameService(db);var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
Pet Add(Player p,int stars,int quality=3,string species="hoa_linh_ho",int slot=0){
    var pet=new Pet{PlayerId=p.Id,DefId=species,Stars=stars,StarPts=PetData.StarAt(stars),Quality=quality,Slot=slot,Level=25,FoodSpent=7};
    p.Pets.Add(pet);db.Pets.Add(pet);return pet;
}
string Snapshot(Player p)=>JsonSerializer.Serialize(new{Pets=p.Pets.OrderBy(x=>x.Id),Food=p.Count("linh_thu_dan")});
void Reject(Player p,Action action,string message){
    var before=Snapshot(p);var rejected=false;
    try{action();}catch(GameException){rejected=true;}
    Check(rejected,message);Check(Snapshot(p)==before,"rejection is atomic: "+message);
}
string Ids(params Pet[] pets)=>string.Join(',',pets.Select(x=>x.Id));
for(var target=1;target<=5;target++){
    var p=new Player{Realm=6};db.Players.Add(p);
    var main=Add(p,target-1,slot:1);var primary=Add(p,target-1);
    var donors=new List<Pet>{primary};
    if(target>1){donors.Add(Add(p,target-2,2,"bang_tinh_tho"));}
    var spare=Add(p,0);var stats=GameService.PetStats(p,main);
    Reject(p,()=>game.MergePets(p,main.Id,null),"empty recipe "+target);
    Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())+","+spare.Id),"excess donor "+target);
    Reject(p,()=>game.MergeAll(p,main.Id),"old merge-all cannot bypass recipe "+target);
    var starBefore=primary.Stars;primary.Stars=target==1?1:target-2;primary.StarPts=PetData.StarAt(primary.Stars);
    Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"wrong primary star "+target);
    primary.Stars=starBefore;primary.StarPts=PetData.StarAt(starBefore);
    primary.Slot=1;Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"active primary "+target);primary.Slot=0;
    primary.DefId="bang_tinh_tho";Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"wrong primary species "+target);primary.DefId=main.DefId;
    var selfDonors=donors.ToArray();selfDonors[0]=main;
    Reject(p,()=>game.MergePets(p,main.Id,Ids(selfDonors)),"self donor "+target);
    if(target>1){
        Reject(p,()=>game.MergePet(p,main.Id,primary.Id),"old single merge cannot bypass recipe "+target);
        Reject(p,()=>game.MergePets(p,main.Id,Ids(primary,primary)),"duplicate donor "+target);
        var other=donors[1];
        foreach(var badQuality in new[]{0,1,4}){other.Quality=badQuality;Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"invalid extra quality "+target+"/"+badQuality);}
        other.Quality=2;other.Slot=1;Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"active extra "+target);other.Slot=0;
        other.Stars=target-1;other.StarPts=PetData.StarAt(other.Stars);Reject(p,()=>game.MergePets(p,main.Id,Ids(donors.ToArray())),"wrong extra star "+target);
        other.Stars=target-2;other.StarPts=PetData.StarAt(other.Stars);
        Reject(p,()=>game.MergePets(p,main.Id,primary.Id+","+Guid.NewGuid()),"unowned donor "+target);
    }
    var refund=donors.Sum(d=>d.FoodSpent/2);var count=p.Pets.Count;
    game.MergePets(p,main.Id,Ids(donors.ToArray()));
    Check(PetData.StarsOf(main)==target&&main.Stars==target,"advance exactly one star "+target);
    Check(p.Pets.Count==count-donors.Count&&p.Pets.Contains(spare),"only selected donors consumed "+target);
    Check(p.Count("linh_thu_dan")==refund,"food refund preserved "+target);
    Check(main.Slot==1&&main.Level==25&&main.Quality==3&&main.FoodSpent==7,"main identity and growth preserved "+target);
    Check(GameService.PetStats(p,main).Atk>stats.Atk,"star improves combat stats "+target);
    await db.SaveChangesAsync();var playerId=p.Id;var petId=main.Id;db.ChangeTracker.Clear();p=await game.LoadAsync(playerId);main=p.Pets.Single(x=>x.Id==petId);
    Check(PetData.StarsOf(main)==target&&p.Pets.Count==2,"save reload preserves result and consumed donors "+target);
    Check(GameService.PetStateFor(p).StarRecipes!.SequenceEqual(PetData.StarRecipes),"client receives server recipes "+target);
    if(target==5)Reject(p,()=>game.MergePet(p,main.Id,spare.Id),"maximum five stars");
}
// Legacy saves with partial points or only Stars retain their existing star tier.
{
    var p=new Player{Realm=6};db.Players.Add(p);var main=Add(p,1);main.StarPts=4;
    Check(PetData.StarsOf(main)==1,"legacy partial points retain tier");
    var primary=Add(p,1);var a=Add(p,0);var b=Add(p,0,2);
    game.MergePets(p,main.Id,Ids(primary,b));Check(PetData.StarsOf(main)==2,"legacy points cannot skip recipe tiers");
    main.Stars=3;main.StarPts=0;Check(PetData.StarsOf(main)==3,"legacy stars-only save retains tier");
    for(var quality=0;quality<=4;quality++){
        main.Quality=quality;main.Stars=2;main.StarPts=PetData.StarAt(2);
        for(var q=0;q<=4;q++){
            var extra=Add(p,1,q,"bang_tinh_tho");
            Check(PetData.CanStarDonor(main,extra,false)==(q<=quality&&q>=Math.Max(0,quality-1)),"all quality boundaries "+quality+"/"+q);
        }
    }
}
Console.WriteLine($"PASS: {checks} pet star recipe, atomic validation, quality, legacy and persistence checks.");
if(args.Length>0){
    var p=new Player{Realm=6};var main=Add(p,4,slot:1);var primary=Add(p,4);
    var a=Add(p,3,3,"bang_tinh_tho");var b=Add(p,3,2,"bang_tinh_tho");
    var higher=Add(p,3,4,"bang_tinh_tho");var lower=Add(p,3,1,"bang_tinh_tho");var active=Add(p,3,3,"bang_tinh_tho",1);
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{petDefs=PetData.Definitions,pet=GameService.PetStateFor(p),main=main.Id,primary=primary.Id,extraA=a.Id,extraB=b.Id,higher=higher.Id,lower=lower.Id,active=active.Id},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));
}
