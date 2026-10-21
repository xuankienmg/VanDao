using VanDao.Game;
using VanDao.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class NoPetRegression
{
    public static IEnumerable<(string Name,Player Player,int Area)> Cases()
    {
        foreach(var realm in new[]{0,1,2,4,6})foreach(var q in new[]{-1,4,9})foreach(var area in new[]{1,15,16})
        {
            var p=new Player{Id=Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),Realm=realm,Stage=Math.Min(1,GameData.Realms[realm].Stages.Length-1),Chapter=Math.Min(realm,5),Diff=realm%3,ExploreReadyAt=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc),LastTickAt=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc)};
            if(q>=0)foreach(var item in GameData.GearBases.Where(x=>x.MinRealm==realm&&x.Slot!=null).GroupBy(x=>x.Slot).Select(g=>g.First()))p.Equipment.Add(new(){PlayerId=p.Id,Slot=item.Slot!,ItemId=GameData.GearId(item,q)});
            if(q>=0){p.SetSkillLevels(SkillData.Skills.Where(x=>x.MinRealm<=realm).ToDictionary(x=>x.Id,x=>x.MaxLv));p.SkillSlots="hoa_cau,"+(SkillData.Skills.FirstOrDefault(x=>x.Tree=="active"&&x.DmgBase>0&&x.MinRealm<=realm)?.Id??"");}
            yield return ($"r{realm}-q{q}-a{area}",p,area);
        }
    }
    public static string Hash(BattleDto battle) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(battle))));
}
