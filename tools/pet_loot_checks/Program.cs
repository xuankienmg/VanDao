using VanDao.Game;

var checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
bool Pill(string id)=>GameData.ItemById.TryGetValue(id,out var item)&&item.Type=="pill";
for(var chapter=0;chapter<GameData.OpenChapters;chapter++)for(var diff=0;diff<3;diff++)for(var area=1;area<=GameData.Areas;area++){
    var c=GameData.Chapters[chapter];var groups=PetLoot.StageGroups(c,diff,area);var odds=PetLoot.StageOdds(c,diff,area);
    Check(groups.All(g=>g.Choices.Length==1&&!Pill(g.Choices[0].Item)),"ordinary stages never drop advancement pills");
    var baseline=new Dictionary<string,double>();var budget=0d;
    void Add(string id,double chance){baseline[id]=1-(1-baseline.GetValueOrDefault(id))*(1-chance);}
    foreach(var d in GameData.DiffDrops(c,diff).Concat(GameData.HerbDropsAt(GameData.DiffHerbChapter(c,diff),area))){
        var chance=GameData.DropChance(d);chance=Math.Min(1,chance*(chance>=1?1:1+(area-1)*.03));
        if(Pill(d.Item)){budget+=chance*.7;Add("linh_thu_dan",chance*.3);}else Add(d.Item,chance);
    }
    Add(c.KeyItem,GameData.KeyChance(area));Add("ruong_thuong",GameData.ChestChance);
    var rare=(area>=GameData.EliteMinArea?GameData.EliteRareChestChance:area>=GameData.RareChestMinArea?GameData.RareChestChance:0)*GameData.DiffRareChest[diff];
    var actual=groups.ToDictionary(g=>g.Choices[0].Item,g=>g.Chance);
    Check(Math.Abs(actual.GetValueOrDefault("ruong_hiem")-Math.Min(1,rare))<1e-12,"rare chest drop rate remains exact");
    var increments=baseline.Where(x=>x.Key!="ruong_hiem").Select(x=>(Before:x.Value,After:actual[x.Key],Delta:actual[x.Key]-x.Value)).ToArray();
    Check(Math.Abs(increments.Sum(x=>x.Delta)-budget)<1e-10,"all removed pill probability is redistributed");
    var uncapped=increments.Where(x=>x.After<1-1e-10).ToArray();
    Check(uncapped.Length==0||uncapped.Max(x=>x.Delta)-uncapped.Min(x=>x.Delta)<1e-10,"equal increments per eligible item");
    Check(increments.All(x=>x.Delta>=0&&x.After<=1),"no negative increments or odds above 100 percent");
    foreach(var g in groups){
        var choice=g.Choices.Single();var shown=odds.Single(x=>x.Item==choice.Item);
        Check(Math.Abs(shown.Chance-g.Chance)<1e-12,"map odds match server roll");
        Check(shown.Min>0&&shown.Max>=shown.Min,"valid displayed quantities");
        if(choice.Item=="linh_thu_dan"){
            Check(shown.Max<=5,"food display capped at five");
            for(var quantity=choice.Min;quantity<=choice.Max;quantity++)
                Check(PetLoot.DropQuantity(choice.Item,quantity,diff)<=5,"actual food drop capped at five after difficulty scaling");
        }
        if(choice.Item==c.KeyItem||choice.Item is "ruong_thuong" or "ruong_hiem")
            Check(shown.Min==1&&shown.Max==1&&PetLoot.QuantityMultiplier(choice.Item,diff)==1,"keys and chests keep one-item quantities at all difficulties");
    }
}
for(var chapter=0;chapter<GameData.OpenChapters;chapter++)for(var diff=0;diff<3;diff++)for(var realm=0;realm<GameData.Realms.Count;realm++){
    var boss=PetLoot.BossMaterials(chapter,diff,realm);
    Check(boss.Count(x=>Pill(x.Item))==1,"boss can drop one correct realm advancement pill type");
    Check(boss.Any(x=>x.Item=="than_thu_huyet")==PetLoot.BloodUnlocked(chapter,diff),"blood progression gate retained");
    foreach(var chest in GameData.Chests.Keys){
        var table=ChestRewards.Table(realm,chest,diff,chapter);
        Check(table.Where(x=>x.Item=="linh_thu_dan").All(x=>x.Qty<=5),"chest food capped at five");
        if(chest==GameData.BossChest){
            var originalEggChance=chapter<2?.15:chapter<3?.12:chapter<6?.10:.08;
            Check(Math.Abs(table.Where(x=>PetData.EggIds.Contains(x.Item)).Sum(x=>x.Probability)-originalEggChance*.25)<1e-12,"boss chest egg chance reduced by 75 percent");
        }
        Check(Math.Abs(table.Sum(x=>x.Probability)-1)<1e-10,"all chest probabilities normalize");
        Check(table.Any(x=>Pill(x.Item))==(chest!="ruong_thuong"),"advancement pills only in rare and boss chests");
        var cd=GameData.Chests[chest];var enlightenment=realm>=GameData.ItemById["ngo_dao_dan"].MinRealm?cd.Enlightenment:0;
        var pill=1-cd.EquipmentChance-cd.Herb-enlightenment;
        if(chest=="ruong_hiem"){
            var scale=(1-enlightenment-.05)/(1-enlightenment);
            Check(Math.Abs(table.Where(x=>x.Group=="pill").Sum(x=>x.Probability)-pill*scale)<1e-12,"rare chest pill chance unchanged");
            Check(Math.Abs(table.Where(x=>x.Group=="gear").Sum(x=>x.Probability)-cd.EquipmentChance*scale)<1e-12,"rare chest gear chance unchanged");
            Check(Math.Abs(table.Where(x=>x.Group=="herb").Sum(x=>x.Probability)-cd.Herb*scale)<1e-12,"rare chest herb chance unchanged");
            Check(Math.Abs(table.Where(x=>x.Group=="enlightenment").Sum(x=>x.Probability)-enlightenment)<1e-12,"rare chest enlightenment chance unchanged");
            Check(Math.Abs(table.Where(x=>x.Group=="pet").Sum(x=>x.Probability)-.05)<1e-12,"rare chest pet chance unchanged");
        }
        if(chest=="ruong_thuong"){
            var removed=pill*(1-enlightenment-.05)/(1-enlightenment);var share=removed/table.Select(x=>x.Item).Distinct().Count();
            Check(Math.Abs(table.Where(x=>x.Item=="linh_thu_dan").Sum(x=>x.Probability)-(.05+share))<1e-12,"common chest food receives equal per-item share");
            if(enlightenment>0)Check(Math.Abs(table.Where(x=>x.Item=="ngo_dao_dan").Sum(x=>x.Probability)-(enlightenment+share))<1e-12,"common chest other eligible items receive same share");
        }
    }
}
Console.WriteLine($"ALL {checks} RESTRICTED PILL LOOT CHECKS PASSED");
