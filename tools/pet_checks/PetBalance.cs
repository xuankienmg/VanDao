using VanDao.Data;
using VanDao.Game;

internal static class PetBalance
{
    public static void Print(Action<bool,string> check)
    {
        Console.WriteLine("BALANCE: full same-realm Huyen (quality 2) gear; peak stage; no learned skills; aptitude 90; no guards.");
        for(var realm=0;realm<GameData.Realms.Count;realm++)
        {
            var p=new Player{Realm=realm,Stage=GameData.Realms[realm].Stages.Length-1};
            foreach(var item in GameData.GearBases.Where(x=>x.MinRealm==realm&&x.Slot!=null&&GameData.Realms[realm].EquipSlots.Contains(x.Slot)).GroupBy(x=>x.Slot).Select(g=>g.First()))
                p.Equipment.Add(new(){PlayerId=p.Id,Slot=item.Slot!,ItemId=GameData.GearId(item,2)});
            var baseline=GameService.Power(p);
            var cap=PetData.LevelCap(realm);
            var form=cap>=60&&realm>=3?2:cap>=30?1:0;
            void Measure(string defId,int petForm)
            {
                p.Pets.Clear();p.Pets.Add(new(){PlayerId=p.Id,DefId=defId,Level=cap,Form=petForm,Slot=1,AptAtk=90,AptDef=90,AptHp=90});
                var power=GameService.Power(p);
                var delta=(power-baseline)*100.0/baseline;
                Console.WriteLine($"BALANCE realm={realm} name={GameData.Realms[realm].Name} pet={defId} level={cap} form={petForm} baseline={baseline} withPet={power} deltaPct={delta:F4}");
                var heavenly=PetData.ById[defId].Rarity=="legendary";
                check(heavenly ? delta>=16 && delta<=20 : delta>=8 && delta<=12,
                    $"balanced {(heavenly?"Heavenly 16–20%":"entry 8–12%")} realm={realm} pet={defId}");
                var stats=GameService.PetStats(p,p.Pets[0]);
                var naked=new Player{Realm=realm,Stage=p.Stage};
                check(stats==GameService.PetStats(naked,p.Pets[0]),$"pet stats independent of owner gear realm={realm} pet={defId}");
                var lower=new Pet{DefId=defId,Level=Math.Max(1,cap-10),Form=petForm,AptAtk=90,AptDef=90,AptHp=90};
                var lowerStats=GameService.PetStats(p,lower);
                check(stats.Atk>lowerStats.Atk&&stats.Def>lowerStats.Def&&stats.Hp>lowerStats.Hp,
                    $"pet level growth retained realm={realm} pet={defId}");
            }
            Measure("hoa_linh_ho",form);
            if(realm>=3)foreach(var def in PetData.Definitions.Where(x=>x.Rarity=="legendary"))Measure(def.Id,2);
        }
    }
}
