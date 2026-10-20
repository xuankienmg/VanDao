using VanDao.Game;
using VanDao.Data;

static class DivineChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static Player Equipped(int quality,int count=10)
    {
        var p=new Player{Id=Guid.Parse("11111111-1111-1111-1111-111111111111"),Realm=3,Chapter=3,Area=15};
        foreach(var b in GameData.GearBases.Where(b=>b.MinRealm==3&&b.Set!=null).GroupBy(b=>b.Slot).Select(g=>g.First()).Take(count))
            p.Equipment.Add(new EquippedItem{Slot=b.Slot!,ItemId=GameData.GearId(b,quality)});
        return p;
    }
    public static void Run()
    {
        for(var q=0;q<10;q++){
            var p=Equipped(q);var a=DivineAbility.For(p);
            Check(a.Tier==(q>=6?q:0),"Only highest full-set quality activates");
            Check(DivineAbility.For(Equipped(q,9)).Tier==0,"Nine pieces cannot activate");
        }
        var mixed=Equipped(9);var last=mixed.Equipment[^1];
        var baseItem=GameData.GearBases.First(b=>b.MinRealm==3&&b.Slot==last.Slot);
        last.ItemId=GameData.GearId(baseItem,6);
        Check(DivineAbility.For(mixed).Tier==6,"Mixed quality uses lowest piece");
        mixed.Equipment.Add(new EquippedItem{Slot=last.Slot,ItemId=last.ItemId});
        Check(DivineAbility.For(mixed).Tier==0,"Duplicate slot disqualifies the set");
        var wrongSet=Equipped(9);var other=GameData.GearBases.First(b=>b.MinRealm==2&&b.Slot==wrongSet.Equipment[^1].Slot);
        wrongSet.Equipment[^1].ItemId=GameData.GearId(other,9);
        Check(DivineAbility.For(wrongSet).Tier==0,"Ten high quality items across different named sets do not activate");
        var tooLow=Equipped(9);tooLow.Realm=0;
        Check(DivineAbility.For(tooLow).Tier==0,"Locked realm equipment cannot activate");
        Check(EquipmentEffects.Calculate(Equipped(8)).Divine!.Progress.Count(p=>p.Active)==1,"Inventory snapshot has one active tier");
        var divine=new DivineCombat(6);
        Check(divine.Receive(10,20)==1&&divine.Saved,"Divine prevents lethal hit once");
        Check(divine.Receive(1,20)==1,"Invulnerable for rest of same turn");
        divine.BeginTurn();Check(divine.Receive(1,20)==-19,"Survival not reset each turn or wave");
        var immortal=new DivineCombat(7);
        Check(immortal.Nullify(new FixedRandom(.149)),"Nullify roll below 15%");
        Check(!immortal.Nullify(new FixedRandom(.15)),"Nullify exact threshold fails");
        Check(new DivineCombat(8).ExtraChance(30)==50,"Dao adds 20 and raises cap");
        Check(new DivineCombat(9).ExtraChance(30)==30,"Supreme does not inherit Dao");
        var supreme=new DivineCombat(9);supreme.Avoided();supreme.Avoided();
        Check(supreme.Amplify(100)==200&&supreme.Amplify(100)==100,"Next attack doubles once without stacking");
        Check(supreme.Nullify(new FixedRandom(.199)),"Supreme nullifies below 20%");
        Check(!supreme.Nullify(new FixedRandom(.20)),"Supreme nullify exact threshold fails");
        Check(new DivineCombat(7).Receive(1,20)==-19,"Immortal does not inherit divine survival");
        var nullSeen=false;var savedSeen=false;var boostSeen=false;
        foreach(var q in new[]{6,7,8,9})for(var seed=1;seed<=40;seed++){
            var p=Equipped(q);p.Id=new Guid(seed,0,0,new byte[8]);p.Chapter=5;p.Diff=3;
            var a=GameService.BattleAt(p,GameData.BossArea,DateTime.UnixEpoch);
            Check(a.Divine!.Tier==q&&a.Divine.Turns.Length==a.Hits,"Real battle selects tier and aligns events");
            if(q==8)Check(a.Divine.ExtraChance==Math.Min(50,GameService.GearEffects(p).ExtraHit+20),"Dao chance used in real battle");
            for(var i=0;i<a.Hits;i++){
                var e=a.Divine.Turns[i];
                if(e.Nullified){nullSeen=true;Check((q==7||q==9)&&!a.Dodged![i]&&!a.MonSkip[i],"Nullification only on incoming non-dodged attack");Check(a.HeroHpLeft[i]-a.Heal![i]==a.HeroHpBefore![i]&&a.GuardLeft![i]==a.GuardBefore![i]&&a.Reflect![i]==0,"Nullification preserves HP/shield, prevents reflection");if(q==9)Check(e.Charged,"Supreme nullification charges next attack");}
                if(e.Saved){savedSeen=true;Check(q==6&&a.HeroHpLeft[i]-a.Heal![i]==1,"Survival leaves one HP before regen");}
                if(e.Boosted){boostSeen=true;Check(q==9,"Only highest tier empowers attack");}
            }
            Check(a.Divine.Turns.Count(e=>e.Saved)<=1,"At most one survival event per battle");
        }
        Check(nullSeen&&savedSeen&&boostSeen,$"Integration must exercise all ability events: null={nullSeen},saved={savedSeen},boost={boostSeen}");
        Console.WriteLine("PASS: divine set eligibility, highest-only tiers, survival once, nullify boundaries, extra cap and next-hit charge.");
    }
    class FixedRandom(double value):Random{public override double NextDouble()=>value;}
}
