using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;

using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
db.Database.OpenConnection();db.Database.EnsureCreated();
var game=new GameService(db);var p=await game.CreateAsync("Evolution checks");
var pet=new Pet{PlayerId=p.Id,DefId="hoa_linh_ho",Level=60,Form=1,Slot=1};p.Pets.Add(pet);db.Pets.Add(pet);
foreach(var cost in PetData.EvolveCost(1))p.Add(cost.Key,cost.Value*3);
var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);checks++;}
void Reject(Action action,string message){try{action();}catch(GameException){Check(true,message);return;}throw new Exception(message);}
p.Realm=2;
var before=p.Count("hoa_hinh_dan");
Check(!GameService.PetStateFor(p).Owned.Single().CanEvolve&&GameService.PetStateFor(p).Owned.Single().EvolveHint.Contains("Nguyên Anh"),"materials alone show missing realm requirement");
Reject(()=>game.Evolve(p,pet.Id),"insufficient realm rejects final evolution");
Check(pet.Form==1&&p.Count("hoa_hinh_dan")==before,"rejected evolution consumes no materials");
p.Realm=3;pet.Level=59;
Check(GameService.PetStateFor(p).Owned.Single().EvolveHint.Contains("60"),"missing pet level is visible");
pet.Level=60;
Check(GameService.PetStateFor(p).Owned.Single().CanEvolve,"final form enabled when every requirement is met");
game.Evolve(p,pet.Id);
Check(pet.Form==2&&p.Count("hoa_hinh_dan")==before-20,"final evolution unlocks form and charges materials once");
var stats=GameService.PetStats(p,pet);var bonus=GameService.PetBonus(p);var food=p.Count("hoa_hinh_dan");
game.SetPetAppearance(p,pet.Id,0);
var dto=GameService.PetStateFor(p).Owned.Single();
Check(dto.Form==2&&dto.AppearanceForm==0&&dto.Image.EndsWith("_0.png"),"infant appearance preserves final evolution");
Check(GameService.PetStats(p,pet)==stats&&GameService.PetBonus(p)==bonus&&p.Count("hoa_hinh_dan")==food,"appearance change is free and preserves combat stats");
await db.SaveChangesAsync();var id=p.Id;db.ChangeTracker.Clear();p=await game.LoadAsync(id);pet=p.Pets.Single();
Check(pet.Form==2&&GameService.PetAppearance(pet)==0,"appearance survives save and reload");
game.SetPetAppearance(p,pet.Id,1);game.SetPetAppearance(p,pet.Id,2);
Check(GameService.PetAppearance(pet)==2,"all unlocked appearances can be selected again");
Reject(()=>game.SetPetAppearance(p,pet.Id,3),"invalid appearance rejected");
pet.Form=0;pet.AppearanceForm=-1;
Reject(()=>game.SetPetAppearance(p,pet.Id,1),"locked appearance rejected");
Check(GameService.PetAppearance(pet)==0,"old saves default to evolved appearance");
var legacyFile=Path.Combine(Path.GetTempPath(),"vandao-appearance-"+Guid.NewGuid()+".db");
try {
    using var legacy=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source="+legacyFile).Options);
    legacy.Database.EnsureCreated();var legacyGame=new GameService(legacy);var oldPlayer=await legacyGame.CreateAsync("Legacy");
    var legacyPet=new Pet{PlayerId=oldPlayer.Id,DefId="hoa_linh_ho",Form=2};oldPlayer.Pets.Add(legacyPet);legacy.Pets.Add(legacyPet);await legacy.SaveChangesAsync();
    legacy.Database.ExecuteSqlRaw("ALTER TABLE Pets DROP COLUMN AppearanceForm;");legacy.ChangeTracker.Clear();
    SchemaUpgrade.Apply(legacy);SchemaUpgrade.Apply(legacy);
    var oldPet=await legacy.Pets.SingleAsync();
    Check(oldPet.Form==2&&oldPet.AppearanceForm==-1&&GameService.PetAppearance(oldPet)==2,"legacy appearance migration is idempotent and preserves evolved form");
} finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(legacyFile);}
Console.WriteLine($"ALL {checks} EVOLUTION CHECKS PASSED");
