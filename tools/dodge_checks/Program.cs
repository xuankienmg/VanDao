using VanDao.Game;
using VanDao.Data;
using System.Text.Json;
DivineChecks.Run();

static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
var burnCheck = new CombatBurn();
var critGear=GameData.ItemById.Values.First(i=>i.Effects?.Any(e=>e.Id=="thien_menh")==true);
var critPlayer=new Player{Realm=critGear.MinRealm};
var critBefore=GameService.Stats(critPlayer).Crit;
critPlayer.Equipment.Add(new EquippedItem{Slot=critGear.Slot!,ItemId=critGear.Id});
var critFx=EquipmentEffects.Calculate(critPlayer).Effects.Single(e=>e.Id=="thien_menh");
Check(critFx.Unit=="%"&&critFx.Cap==60,"Thiên mệnh is a percentage chance bonus capped at 60");
Check(GameService.Stats(critPlayer).Crit==Math.Min(100,critBefore+critGear.CritPct+critFx.Effective),"Thiên mệnh increases actual crit chance");
Check(critGear.Effects!.Single(e=>e.Id=="thien_menh").Text.Contains("Tỉ lệ bạo kích"),"Item tooltip matches chance behavior");
Console.WriteLine("PASS: Thiên mệnh adds crit chance and reports percentage units.");
Check(burnCheck.Tick(1000,false)==0,"No burn before trigger");
Check(burnCheck.Tick(1000,true)==50,"Burn deals 5% max HP on trigger");
Check(burnCheck.Tick(1000,false)==50,"Burn persists a second turn");
Check(burnCheck.Tick(1000,false)==0,"Burn expires after two turns");
Check(burnCheck.Tick(1000,true)==50,"Burn retriggers");
Check(burnCheck.Tick(1000,true)==50,"Retrigger refreshes without stacking damage");
Check(burnCheck.Tick(1000,false)==50,"Refreshed burn lasts another turn");
Check(burnCheck.Tick(1000,false)==0,"Refreshed burn expires");
burnCheck.Tick(1000,true);burnCheck.Reset();
Check(burnCheck.Tick(2000,false)==0,"New monster clears burn");
Check(burnCheck.Tick(2000,true)==100,"Burn uses new monster max HP");
Console.WriteLine("PASS: burn 5% max HP, two turns, refresh without stacking, wave reset.");
Check(CombatDodge.Chance(10,4,3)==17,"All dodge sources must add");
Check(CombatDodge.Chance(60,40,20)==50,"Dodge must cap at 50%");
Check(!CombatDodge.Roll(new ConstantRandom(0),0),"0% cannot dodge");
Check(CombatDodge.Roll(new ConstantRandom(.049),5),"Roll below threshold must dodge");
Check(!CombatDodge.Roll(new ConstantRandom(.05),5),"Roll at threshold must hit");
var found=false;
for(var seed=1;seed<=80;seed++){
    var p=new Player{Id=new Guid(seed,0,0,new byte[8]),Realm=1,Stage=0,Chapter=1,Area=15,BodyHp=30000,ExploreReadyAt=DateTime.UnixEpoch};
    foreach(var slot in new[]{"head","ring"}){
        var gear=GameData.GearBases.First(g=>g.MinRealm==1&&g.Slot==slot);
        p.Equipment.Add(new EquippedItem{PlayerId=p.Id,Slot=slot,ItemId=GameData.GearId(gear,3)});
    }
    var a=GameService.BattleAt(p,15,DateTime.UnixEpoch);
    var b=GameService.BattleAt(p,15,DateTime.UnixEpoch);
    Check(JsonSerializer.Serialize(a)==JsonSerializer.Serialize(b),"Same trip must reproduce dodge and damage");
    Check(a.Dodged is not null&&a.Dodged.Length==a.Hits,"Dodge flags must align with sampled turns");
    Check(a.DodgeChance==GameService.Stats(p).Speed,"Thân pháp must contribute to dodge chance");
    Check(a.Guard>0&&GameService.GearEffects(p).Reflect>0,"Integration fixture must have real shield and reflection");
    for(var i=0;i<a.Hits;i++)if(a.Dodged![i]){
        found=true;Check(!a.MonSkip[i],"Stun/dead enemy is not a dodge");
        var oldHp=a.HeroHpBefore![i];
        var oldGuard=a.GuardBefore![i];
        Check(a.HeroHpLeft[i]==oldHp,"Dodged attack must not damage HP");
        Check(a.GuardLeft![i]==oldGuard,"Dodged attack must not consume shield");
        Check(a.Reflect![i]==0,"Dodged attack must not trigger reflection");
    }
}
Check(found,"Seeded integration cases must exercise actual dodges");
var burnFound=false;
for(var seed=1;seed<=20;seed++){
    var p=new Player{Id=new Guid(seed,0,0,new byte[8]),Realm=1,Stage=0,Chapter=1,Area=15,BodyHp=30000,Skills="phan_cot:1"};
    var battle=GameService.BattleAt(p,15,DateTime.UnixEpoch);
    Check(battle.BurnPct==5,"Burn HUD must report fixed 5%");
    Check(battle.BurnDamage!.Length==battle.Hits,"Burn damage aligns with sampled turns");
    for(var i=0;i<battle.Hits;i++){
        var amount=battle.BurnDamage[i];
        Check(battle.Burned![i]==(amount>0),"Burn visuals match damage");
        if(amount==0)continue;
        var hp=battle.Waves!.Last(w=>w.Start<=i).Hp;
        Check(amount==Math.Max(1,(int)Math.Round(hp*.05)),"Each wave burns exactly 5% max HP");
        burnFound=true;
    }
}
Check(burnFound,"Integration must exercise burning");
Console.WriteLine("PASS: real battles expose fixed 5% burn damage per monster and aligned visual flags.");
var zero=new Player{Id=Guid.Empty,Realm=0,Stage=0,BodyHp=30000};
var zeroBattle=GameService.BattleAt(zero,15,DateTime.UnixEpoch);
Check(zeroBattle.DodgeChance==0&&zeroBattle.Dodged!.All(v=>!v),"No Thân pháp/gear must yield zero dodges");
Console.WriteLine("PASS: additive chance, 50% cap, exact roll boundaries, seeded battles, turn alignment, no HP/shield loss/reflection on dodge, zero chance.");
class ConstantRandom(double value):Random{public override double NextDouble()=>value;}
