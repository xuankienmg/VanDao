using System.Collections.Concurrent;

namespace VanDao.Game;

public record ChestReward(string Item, int Qty, double Probability, string Group, string? BaseItem = null);

/// <summary>One immutable distribution shared by rolling and displayed odds, cached per realm/chest.</summary>
public static class ChestRewards
{
    private static readonly ConcurrentDictionary<(int Realm,string Chest),IReadOnlyList<ChestReward>> Cache=new();
    public static IReadOnlyList<ChestReward> Table(int realm,string chest) => Cache.GetOrAdd((realm,chest),key=>Build(key.Realm,key.Chest));
    public static ChestReward Roll(int realm,string chest,Random rng)
    {
        var table=Table(realm,chest);var roll=rng.NextDouble();
        foreach(var reward in table)if((roll-=reward.Probability)<0)return reward;
        return table[^1];
    }
    private static IReadOnlyList<ChestReward> Build(int realm,string chest)
    {
        if(realm<0||realm>=GameData.Realms.Count)throw new GameException("Cảnh giới không hợp lệ.");
        var cd=GameData.Chests[chest];var r=GameData.Realms[realm];var list=new List<ChestReward>();
        var gear=GameData.GearBases.Where(b=>b.Slot!=null&&b.MinRealm==realm).ToArray();
        if(gear.Length==0)throw new InvalidOperationException("Chưa cấu hình trang bị rương đúng cảnh giới.");
        var equipmentChance=cd.EquipmentChance;
        var enlightenment=realm>=GameData.ItemById["ngo_dao_dan"].MinRealm?cd.Enlightenment:0;
        var pillChance=1-equipmentChance-cd.Herb-enlightenment;
        var quality=cd.QualityAt(realm);var qs=quality.Sum();
        if(qs<=0||quality.Any(q=>q<0)||pillChance<0)throw new InvalidOperationException("Bảng thưởng rương không hợp lệ.");
        void Add(string item,int qty,double probability,string group,string? baseItem=null){if(probability>0)list.Add(new(item,qty,probability,group,baseItem));}
        for(var q=0;q<quality.Length;q++)if(quality[q]>0){
            foreach(var g in gear)Add(GameData.GearId(g,q),1,equipmentChance/gear.Length*quality[q]/qs,"gear",g.Id);
        }
        var pill=r.StageCost(0).Keys.First(k=>k!="linh_thach");
        for(var n=cd.PillMin;n<=cd.PillMax;n++)Add(pill,n,pillChance/(cd.PillMax-cd.PillMin+1),"pill");
        var herbs=GameData.HerbDrops.Where(h=>h.Chapters.Contains(realm)).GroupBy(h=>h.Item).Select(g=>new{Item=g.Key,Weight=g.Sum(h=>h.Chance)}).ToArray();
        if(cd.Herb>0&&(herbs.Length==0||herbs.Sum(h=>h.Weight)<=0))throw new InvalidOperationException("Chưa cấu hình linh thảo cho rương.");
        foreach(var herb in herbs)for(var n=cd.HerbMin;n<=cd.HerbMax;n++)Add(herb.Item,n,cd.Herb*herb.Weight/herbs.Sum(h=>h.Weight)/(cd.HerbMax-cd.HerbMin+1),"herb");
        Add("ngo_dao_dan",1,enlightenment,"enlightenment");
        if(Math.Abs(list.Sum(x=>x.Probability)-1)>1e-8||list.Any(x=>!GameData.ItemById.ContainsKey(x.Item)))throw new InvalidOperationException("Tổng xác suất rương không hợp lệ.");
        return list.AsReadOnly();
    }
}
