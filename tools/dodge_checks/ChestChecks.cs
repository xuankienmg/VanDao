using VanDao.Game;
using VanDao.Data;
using Microsoft.EntityFrameworkCore;
static class ChestChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(){
        foreach(var chest in GameData.Chests.Keys)for(var realm=0;realm<7;realm++){
            var table=ChestRewards.Table(realm,chest);
            Check(table.Where(x=>x.Group is "gear" or "treasure").All(x=>GameData.ItemById[x.Item].MinRealm==realm),"All chest gear and treasures match character realm");
            Check(Math.Abs(table.Sum(x=>x.Probability)-1)<1e-8,"Chest reward probabilities sum to 1");
            Check(!table.Any(x=>x.Group=="treasure"),"Treasures use the unified gear group");
            var gearChance=chest=="ruong_hiem"?.57:chest=="ruong_boss"?.67:.46;
            Check(Math.Abs(table.Where(x=>x.Group=="gear").Sum(x=>x.Probability)-gearChance)<1e-8,"Normal gear chance stays unchanged");
            var perItem=table.Where(x=>x.Group=="gear").GroupBy(x=>x.BaseItem).Select(g=>g.Sum(x=>x.Probability)).ToArray();
            Check(perItem.Max()-perItem.Min()<1e-8,"Each gear base, including offhand, has equal chance");
            var herbChance=chest=="ruong_thuong"?.20:chest=="ruong_hiem"?.265:.195;
            Check(Math.Abs(table.Where(x=>x.Group=="herb").Sum(x=>x.Probability)-herbChance)<1e-8,"30 percent of removed treasure chance is split between herbs and stage pills");
            var expected=realm<3?0:chest=="ruong_thuong"?.03:chest=="ruong_hiem"?.04:.05;
            Check(Math.Abs(table.Where(x=>x.Item=="ngo_dao_dan").Sum(x=>x.Probability)-expected)<1e-8,"Enlightenment pill gate and 3/4/5 percent rates");
            Check(table.Where(x=>x.Group=="herb").All(x=>GameData.HerbDrops.Any(h=>h.Item==x.Item&&h.Chapters.Contains(realm))),"Herbs match realm chapter pool");
            Check(table.Where(x=>x.Group=="enlightenment").All(x=>x.Qty==1),"Enlightenment grants one pill");
            Check(GameData.Chests[chest].QualityAt(realm).Length==10,"Explicit ten quality slots for each realm");
            var odds=GameService.ChestOdds(new Player{Realm=realm}).Single(o=>o.Chest==chest);
            Check(Math.Abs(odds.Herb-table.Where(x=>x.Group=="herb").Sum(x=>x.Probability)*100)<.001,"HUD herb chance equals actual distribution");
            Check(odds.Enlightenment==expected*100,"HUD enlightenment matches actual distribution");
            var rng=new Random(9281+realm);int enlightenment=0;
            for(var i=0;i<100000;i++){var reward=ChestRewards.Roll(realm,chest,rng);if(reward.Item=="ngo_dao_dan")enlightenment++;}
            Check(Math.Abs(enlightenment/100000.0-expected)<.004,"Actual rolling agrees with enlightenment rate");
            if(realm<3&&chest=="ruong_thuong")Check(GameData.Chests[chest].QualityAt(realm)[4]==0,"No early normal-chest heavenly gear");
            if(realm<3)Check(GameData.Chests[chest].QualityAt(realm)[6]==0,"No early divine gear");
            if(realm<5)Check(GameData.Chests[chest].QualityAt(realm)[8]==0,"No Dao before Luyen Hu");
            Check(GameData.Chests[chest].QualityAt(realm)[9]==0,"No supreme chest drops");
        }
        foreach(var it in GameData.AllItems.Where(i=>i.Slot!=null)){
            var q=Array.IndexOf(GameData.RarityOrder,it.Rarity);var mult=q switch{5=>1.25,6=>1.5,7=>1.75,8=>2.0,9=>2.5,_=>1};
            var price=(int)Math.Round(GameData.RarityPrice[it.Rarity]*(1+it.MinRealm)*mult,MidpointRounding.AwayFromZero);
            Check(it.Sell==price,"High quality gear prices match approved multipliers");
        }
        using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);var game=new GameService(db);
        var p=new Player{Realm=3};p.Add("ruong_hiem",3);var result=game.UseItem(p,"ruong_hiem",3);
        Check(p.Count("ruong_hiem")==0,"Batch opening consumes requested chests");
        Check(result.craft!.Inputs.Single().Qty==3,"Batch reports actual consumed chests");
        var full=new Player{Realm=0};
        foreach(var it in GameData.AllItems.Where(i=>i.Slot!=null).Take(100))full.Add(it.Id,1);
        full.Add("ruong_thuong",1);var before=full.Inventory.ToDictionary(i=>i.ItemId,i=>i.Count);
        try{game.UseItem(full,"ruong_thuong",1);}catch(GameException){}
        Check(full.Count("ruong_thuong")==1,"Full bag does not consume chest");
        Check(before.All(kv=>full.Count(kv.Key)==kv.Value),"Failed chest opening preserves existing inventory");
        var high=GameData.AllItems.First(i=>i.Slot!=null&&i.Rarity=="supreme");var rich=new Player();rich.Add(high.Id,1000);rich.Add("linh_thach",int.MaxValue-1);
        bool rejected=false;try{game.Sell(rich,high.Id,1);}catch(GameException){rejected=true;}
        Check(rejected&&rich.Count(high.Id)==1000&&rich.Count("linh_thach")==int.MaxValue-1,"Single sale overflow rejects without consuming items");
        rejected=false;try{game.SellBulk(rich,9,pick:true);}catch(GameException){rejected=true;}
        Check(rejected&&rich.Count(high.Id)==1000,"Bulk sale overflow rejects atomically");
        Console.WriteLine("PASS 2.1M chest rolls, five branches, realm gates, odds consistency, gear prices, batch and full-bag rollback.");
    }
}
