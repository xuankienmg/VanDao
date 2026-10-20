using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Game;
static class SellChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(){
        Check(GameData.AllItems.Where(i=>i.Type!="stone").All(i=>i.Sell>0),"Every non-currency item has a sale price");
        var ticket=GameData.ItemById[GameData.SpinTicket];
        Check(!GameService.Tradable(ticket),"Tai Xiu tickets blocked by default");
        foreach(var dev in new[]{false,true})foreach(var admin in new[]{false,true}){
            var seller=new Player{Tainted=admin};
            Check(GameService.CanSellSpinTickets(seller,dev)==(dev&&admin),"Ticket selling requires admin AND Development");
            Check(GameService.Tradable(ticket,GameService.CanSellSpinTickets(seller,dev))==(dev&&admin),"Market enforces ticket permission");
        }
        Check(!GameService.Tradable(GameData.ItemById["linh_thach"]),"Currency still excluded from market");
        var items=GameData.AllItems.Where(i=>new[]{"herb","key","chest","ticket"}.Contains(i.Type)).GroupBy(i=>i.Type).Select(g=>g.First()).ToArray();
        var p=new Player();p.Add("linh_thach",100);
        foreach(var i in items)p.Add(i.Id,2);
        using var db=new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite("Data Source=:memory:").Options);
        var game=new GameService(db);
        var gain=items.Sum(i=>i.Sell*2);
        game.SellBulk(p,9,items:string.Join(',',items.Select(i=>i.Id+":2")),pick:true);
        Check(items.All(i=>p.Count(i.Id)==0),"Bulk sells selected non-equipment stacks");
        Check(p.Count("linh_thach")==100+gain,"Bulk proceeds match displayed price");
        Console.WriteLine("PASS all-item selling, exact proceeds and Tai Xiu ticket market eligibility.");
    }
}
