using System.Text.Json;
using VanDao.Data;
using VanDao.Game;

var count=0;
void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
var owner=new CombatStats(1000,10000,100);
PetCombat Combat(string id,int form)=>new(PetData.ById[id],form,owner);
for(var form=0;form<3;form++){
    double V(double a,double b,double c)=>form==0?a:form==1?b:c;
    PetCastDto Cast(PetCombat combat,ref int hp,int def=0,int monsterHp=100000)=>combat.Cast(3,ref hp,monsterHp,100000,def,1);
    var hp=1000;var fox=Combat("hoa_linh_ho",form);var hit=Cast(fox,ref hp);
    Check(hit.Dmg==(int)(owner.Atk*V(70,105,140)/100),"fox direct damage");
    fox.BeginTurn(4,ref hp,10000,0,1);Check(fox.AmplifyOwner(1000,true,false)==(int)(1000*(1+V(5,8,12)/100)),"fox boosts only following critical hits");
    Check(fox.AmplifyOwner(1000,false,false)==1000,"fox leaves normal hit unchanged");
    fox.BeginTurn(6,ref hp,10000,0,1);Check(fox.AmplifyOwner(1000,true,false)==1000,"fox mark expires after two future turns");
    var frost=Combat("bang_tinh_tho",form);hit=Cast(frost,ref hp);
    Check(hit.Guard==(int)(owner.Hp*V(6,9,12)/100)&&frost.AttackDown==V(5,8,12),"frost shield and attack debuff");
    frost.BeginTurn(4,ref hp,10000,0,1);Check(frost.AttackDown>0,"frost second turn");
    frost.BeginTurn(5,ref hp,10000,0,1);Check(frost.AttackDown==0,"frost expires");
    var deer=Combat("thanh_moc_loc",form);hp=1000;hit=Cast(deer,ref hp);
    Check(hit.Heal==(int)Math.Round(owner.Hp*V(5,8,11)/100*1.15),"deer low health healing talent");
    for(var t=4;t<=5;t++)Check(deer.BeginTurn(t,ref hp,10000,0,1)!.Heal>0,"deer two future healing ticks");
    Check(deer.BeginTurn(6,ref hp,10000,0,1)==null,"deer hot ends");
    Check(deer.Healing(100,0)==0&&deer.Healing(10000,9999)==1,"healing never revives or overheals");
    var turtle=Combat("kim_giap_quy",form);hit=Cast(turtle,ref hp);
    Check(hit.Guard==(int)(owner.Hp*V(8,12,16)/100)&&turtle.GuardBonus==V(3,5,7),"turtle shielding and mitigation");
    turtle.Absorb(100000);Check(turtle.GuardBonus==0,"turtle mitigation ends when own shield breaks");
    var eagle=Combat("loi_van_dieu",form);hit=Cast(eagle,ref hp,100);
    Check(hit.Hits!.Length==3&&hit.Hits.All(h=>h==(int)(owner.Atk*V(25,38,52)/100-100))&&hit.Dmg==hit.Hits.Sum(),"eagle three separately defended strikes");
    var leech=Combat("bich_thuy_giao",form);hp=1000;hit=Cast(leech,ref hp,0,50);
    Check(hit.Dmg==50&&hit.Heal==(int)(50*V(20,25,30)/100),"leech only heals from actual damage, no overkill");
    var crane=Combat("thanh_phong_hac",form);hp=1000;hit=Cast(crane,ref hp);
    Check(hit.Heal==(int)(owner.Hp*V(4,6,8)/100)&&crane.DodgeBonus==V(5,8,12),"crane heal and dodge");
    crane.BeginTurn(5,ref hp,10000,0,1);Check(crane.DodgeBonus==0,"crane dodge expires");
    var cat=Combat("tu_dien_mieu",form);hit=Cast(cat,ref hp,100);
    Check(hit.Dmg==(int)(owner.Atk*V(90,135,180)/100-Math.Floor(100*(1-V(15,25,35)/100))),"cat armor penetration");
    var phoenix=Combat("chu_tuoc",form);hit=Cast(phoenix,ref hp);
    Check(hit.Dmg==(int)(owner.Atk*V(80,120,160)/100)&&phoenix.Burning,"phoenix direct damage and burn");
    Check(phoenix.AmplifyOwner(1000,false,true)==1100&&phoenix.AmplifyOwner(1000,false,false)==1000,"phoenix talent only on burning targets");
    for(var t=4;t<=5;t++)Check(phoenix.BeginTurn(t,ref hp,10000,0,1)!.Dmg==(int)(owner.Atk*V(15,25,35)/100),"phoenix two non-stacking burn ticks");
    Check(phoenix.BeginTurn(6,ref hp,10000,0,1)==null,"phoenix burn expires");
    Cast(phoenix,ref hp);phoenix.NextWave();Check(!phoenix.Burning,"burn does not transfer to new enemy");
    var xuanwu=Combat("huyen_vu",form);Check(xuanwu.InitialShield()==1000,"xuanwu initial shield");hit=Cast(xuanwu,ref hp);
    Check(xuanwu.Reflect(3,10000,0,1)!.Dmg==(int)(owner.Atk*V(15,25,35)/100),"xuanwu attack based reflect");
    Check(xuanwu.Reflect(5,10000,0,1)==null,"reflect expires");
    for(var n=0;n<20;n++)Cast(xuanwu,ref hp);Check(xuanwu.Shield==3000,"pet shields capped at 30 percent");
    var dragon=Combat("thanh_long",form);hit=Cast(dragon,ref hp,100);
    Check(hit.Hits!.Length==3&&hit.Hits[0]==(int)(owner.Atk*V(35,55,75)/100-100),"dragon three lightning strikes");
    Check(hit.Hits[2]-hit.Hits[0]==(form==2?20:0),"divine dragon last bolt penetrates twenty percent defense");
    Check(hit.Dmg==hit.Hits.Sum(),"dragon damage matches separate strikes");
    var tiger=Combat("bach_ho",form);hit=Cast(tiger,ref hp,0,20000);
    Check(hit.Dmg==(int)(owner.Atk*V(110,165,220)/100*1.25),"tiger execute bonus");
    hit=Cast(tiger,ref hp,0,30000);Check(hit.Dmg==(int)(owner.Atk*V(110,165,220)/100),"execute strictly below thirty percent");
}
Check(PetData.Definitions.Select(d=>d.Skill).Distinct().Count()==12,"twelve unique active skills");
Check(PetData.ById["thanh_long"].Element=="loi"&&PetData.ById["thanh_long"].Talent=="atk","dragon changes element and talent without changing identity");
for(var form=0;form<3;form++)Check(PetData.ById["thanh_long"].Forms[form].Image.Contains("_loi_"),"new dragon portraits are referenced");
foreach(var def in PetData.Definitions){
    var p=new Player{Id=Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),Realm=0,Chapter=1,BodyHp=1000000,ExploreReadyAt=new DateTime(2026,10,21,0,0,0,DateTimeKind.Utc)};
    var baseline=JsonSerializer.Serialize(GameService.BattleAt(p,15));
    var pet=new Pet{PlayerId=p.Id,DefId=def.Id,Form=2,Level=20,Slot=0};p.Pets.Add(pet);
    Check(JsonSerializer.Serialize(GameService.BattleAt(p,15))==baseline,"resting pet does not change battle: "+def.Id);
    Check(GameService.PetTalent(p,def.Talent)==0,"resting talent inactive: "+def.Id);
    pet.Slot=1;Check(GameService.PetTalent(p,def.Talent)==def.TalentValue,"active talent applied: "+def.Id);
    var battle=GameService.BattleAt(p,15);
    Check(JsonSerializer.Serialize(battle)==JsonSerializer.Serialize(GameService.BattleAt(p,15)),"deterministic replay: "+def.Id);
    Check(battle.Pet!.Casts.Length>0,"new active skill produces casts: "+def.Id);
    Check(battle.Pet.Casts.All(c=>c.Turn>=0&&c.Turn<battle.Hits&&c.Dmg>=0&&c.Heal>=0&&c.Guard>=0),"cast bounds: "+def.Id);
    Check(battle.HeroHpLeft.All(h=>h>=0&&h<=battle.HeroHp),"health bounds: "+def.Id);
}
foreach(var def in PetData.Definitions)for(var form=0;form<3;form++){
    var previous=-1;
    for(var stars=0;stars<=5;stars++){
        var pet=new Pet{DefId=def.Id,Form=form,Stars=stars,StarPts=PetData.StarAt(stars),Slot=1,Level=60};
        var p=new Player{Realm=6};p.Pets.Add(pet);
        Check(Math.Abs(GameService.PetTalent(p,def.Talent)-def.TalentValue*(1+stars*.06))<1e-10,"star-scaled talent: "+def.Id+"/"+stars);
        var dto=GameService.PetStateFor(p).Owned.Single();
        Check(!dto.SkillText.Contains('/'),"description only shows current form and stars: "+def.Id);
        Check(stars==5?dto.NextStarSkillText==""&&dto.NextStarTalentText=="":dto.NextStarSkillText!=""&&dto.NextStarTalentText!="","next-star previews capped at five");
        var pc=new PetCombat(def,form,owner,stars);var hp=1000;pc.InitialShield();
        var cast=pc.Cast(3,ref hp,100000,100000,0,1);var strength=cast.Dmg+cast.Heal+pc.Shield+(pc.Reflect(3,100000,0,1)?.Dmg??0);
        Check(strength>previous,"actual skill power grows at each star: "+def.Id+"/"+form+"/"+stars);previous=strength;
        pet.Slot=0;Check(GameService.PetTalent(p,def.Talent)==0,"starred resting pet talent remains inactive");
    }
}
Check(PetData.SkillDescription("xuanwu",0,0)=="Tạo khiên 10% máu tối đa. Trong 2 lượt, khi bị đánh phản 15% công.","requested infant shield description");
Check(PetData.SkillDescription("xuanwu",0,1)=="Tạo khiên 10,6% máu tối đa. Trong 2 lượt, khi bị đánh phản 15,9% công.","one-star shield and reflect percentages");
{
    var pc=new PetCombat(PetData.ById["huyen_vu"],0,owner,1);var hp=1000;
    Check(pc.InitialShield()==1060,"initial shield talent scales with stars");
    Check(pc.Cast(3,ref hp,100000,100000,0,1).Guard==1060,"skill shield matches displayed 10.6 percent");
    Check(pc.Reflect(3,100000,0,1)!.Dmg==159,"reflection matches displayed 15.9 percent");
}
Console.WriteLine($"PASS: {count} three-form skill, talent, star growth, display, duration, replay and integration checks.");
if(args.Length>0){
    var p=new Player{Realm=6,BodyHp=10000};
    foreach(var def in PetData.Definitions)p.Pets.Add(new(){PlayerId=p.Id,DefId=def.Id,Form=2,Level=60,Slot=def.Id=="thanh_long"?1:0});
    var starSamples=new List<PetDto>();var samplePlayer=new Player{Realm=6};
    var samplePet=new Pet{DefId="huyen_vu",Form=0,Slot=1};samplePlayer.Pets.Add(samplePet);
    for(var star=0;star<=5;star++){samplePet.Stars=star;samplePet.StarPts=PetData.StarAt(star);starSamples.Add(GameService.PetStateFor(samplePlayer).Owned.Single());}
    File.WriteAllText(args[0],JsonSerializer.Serialize(new{petDefs=PetData.Definitions,pet=GameService.PetStateFor(p),starSamples},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));
}
