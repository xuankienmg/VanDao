using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using VanDao.Data;
using VanDao.Desktop;
using VanDao.Game;

namespace VanDao;

public static class Program
{
    /// <summary>
    /// Mặc định: chạy server nội bộ và mở cửa sổ game nổi (kiểu picture-in-picture).
    /// Thêm tham số --web để chỉ chạy server và chơi trên trình duyệt như trước.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        var webOnly = args.Contains("--web");
        var app = BuildServer(args.Where(a => a != "--web").ToArray(), webOnly);

        if (webOnly)
        {
            app.Run();
            return;
        }

        app.StartAsync().GetAwaiter().GetResult();
        var url = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GameWindow(url));

        app.StopAsync().GetAwaiter().GetResult();
    }

    private static WebApplication BuildServer(string[] args, bool webOnly)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            // để vandao.db và wwwroot luôn nằm cạnh file chạy, dù khởi động từ đâu
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Chế độ cửa sổ: chỉ nghe trên máy này, cổng ngẫu nhiên để không đụng app khác
        if (!webOnly) builder.WebHost.UseUrls("http://127.0.0.1:0");

        // DB nằm trong %LocalAppData%\VanDao để không mất khi build lại hay xóa bin/
        var dbPath = Path.Combine(AppPaths.DataDir, "vandao.db");
        builder.Services.AddDbContext<GameDbContext>(o =>
            o.UseSqlite(builder.Configuration.GetConnectionString("Game") ?? $"Data Source={dbPath}"));
        builder.Services.AddScoped<GameService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            db.Database.EnsureCreated();
            SchemaUpgrade.Apply(db);
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        MapApi(app);
        return app;
    }

    private static void MapApi(WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/data", () => new GameDataDto(
            GameData.AllItems,
            GameData.Slots,
            GameData.ExtraStats,
            GameData.Realms.Select((r, i) => new RealmInfoDto(i, r.Id, r.Name, r.Desc, r.Stages, r.Aura, r.Lotus, r.Sash, r.Hero, r.Resources, r.Cn, r.BagSlots, r.EquipSlots,
                r.Boss.Name, r.Boss.Image, GameData.ChapterDropping(r.BossItem)?.BossLair ?? "", GameData.ChapterDropping(r.BossItem)?.KeyItem ?? "", r.BossItem,
                r.HeroStand, r.HeroMed, r.HeroCast, r.Halo)),
            GameData.Synths, SkillData.Elements, SkillData.Skills, GameData.PillChain, GameData.Areas,
            GameData.LockedRealms, GameData.FuseChance,
            GameData.Chapters.Select(c => new ChapterInfoDto(c.Index, c.Name, c.Realm, c.Enabled, c.Boss.Name, c.Boss.Image, c.BossLair, c.KeyItem, c.BossItem)),
            GameData.Brews, GameData.HerbDrops, GameData.Skins, GameData.SpinWeights, GameData.SpinTicketPrice));

        api.MapPost("/players", async (CreatePlayerRequest? req, GameService game) =>
        {
            var p = await game.CreateAsync(req?.Name);
            return Results.Ok(await game.BuildStateAsync(p));
        });

        api.MapGet("/players/current", async (GameService game) =>
        {
            var p = await game.CurrentAsync();
            await game.SaveAsync(p);
            return Results.Ok(new ActionResponse(await game.BuildStateAsync(p)));
        });

        api.MapGet("/players/{id:guid}", (Guid id, GameService game) =>
            Run(id, game, _ => (null, null)));

        api.MapPost("/players/{id:guid}/channel", (Guid id, GameService game) =>
            Run(id, game, p => (game.Channel(p), null)));

        api.MapPost("/players/{id:guid}/equip/{itemId}", (Guid id, string itemId, string? slot, GameService game) =>
            Run(id, game, p => game.Equip(p, itemId, slot)));

        api.MapPost("/players/{id:guid}/use/{itemId}", (Guid id, string itemId, int? n, GameService game) =>
            RunCraft(id, game, p => game.UseItem(p, itemId, n ?? 1)));

        api.MapPost("/players/{id:guid}/drop/{itemId}", (Guid id, string itemId, int? n, GameService game) =>
            Run(id, game, p => game.Drop(p, itemId, n ?? 1)));

        api.MapPost("/players/{id:guid}/skill/{skillId}", (Guid id, string skillId, GameService game) =>
            Run(id, game, p => game.UpgradeSkill(p, skillId)));

        // trang phục: skin = id trong GameData.Skins, rỗng = theo cảnh giới
        api.MapPost("/players/{id:guid}/skin", (Guid id, string? skin, GameService game) =>
            Run(id, game, p => game.SetSkin(p, skin)));

        api.MapPost("/players/{id:guid}/skillslot/{slot:int}", (Guid id, int slot, string? skill, GameService game) =>
            Run(id, game, p => game.SetSkillSlot(p, slot, skill)));

        api.MapPost("/players/{id:guid}/sell/{itemId}", (Guid id, string itemId, int? n, GameService game) =>
            Run(id, game, p => game.Sell(p, itemId, n ?? 1)));

        api.MapPost("/players/{id:guid}/sell-bulk", (Guid id, int max, bool? gear, string? items, bool? pick, GameService game) =>
            Run(id, game, p => game.SellBulk(p, max, gear ?? false, items, pick ?? false)));

        api.MapPost("/players/{id:guid}/unequip/{slot}", (Guid id, string slot, GameService game) =>
            Run(id, game, p => game.Unequip(p, slot)));

        api.MapPost("/players/{id:guid}/advance", (Guid id, GameService game) =>
            Run(id, game, p => game.Advance(p)));

        // luyện khí: ghép 9 trang bị cùng phẩm → 1 phẩm cao hơn (items = danh sách id, lặp lại khi lấy nhiều món cùng id)
        api.MapPost("/players/{id:guid}/fuse", (Guid id, string? items, GameService game) =>
            RunCraft(id, game, p => game.Fuse(p, items)));

        // code quà tặng (nhập ở menu Cài đặt)
        api.MapPost("/players/{id:guid}/redeem", (Guid id, string? code, GameService game) =>
            Run(id, game, p => game.RedeemCode(p, code)));

        // Tài Xỉu: đổi linh thạch lấy vé (n = số vé, 0 = tối đa) · quay vòng trang bị 10 phẩm (n = số lượt, tối đa 10)
        api.MapPost("/players/{id:guid}/spin/buy", (Guid id, int? n, GameService game) =>
            Run(id, game, p => game.BuySpinTicket(p, n ?? 1)));
        api.MapPost("/players/{id:guid}/spin", (Guid id, int? n, GameService game) =>
            RunCraft(id, game, p => game.Spin(p, n ?? 1)));

        // đan dược: hợp thành 9 → 1 (n = số lần, 0 = tối đa)
        api.MapPost("/players/{id:guid}/synth/{itemId}", (Guid id, string itemId, int? n, GameService game) =>
            RunCraft(id, game, p => game.Synthesize(p, itemId, n ?? 1)));

        // luyện đan theo đan phương (linh dược + đan + linh thạch → 1 viên), n = số lần, 0 = tối đa
        api.MapPost("/players/{id:guid}/brew/{itemId}", (Guid id, string itemId, int? n, GameService game) =>
            RunCraft(id, game, p => game.Brew(p, itemId, n ?? 1)));

        // bản đồ: chọn ải 1..15 của chương đang đánh, 16 = Boss (cần lệnh Boss)
        api.MapPost("/players/{id:guid}/area/{area:int}", (Guid id, int area, GameService game) =>
            Run(id, game, p => game.SelectArea(p, area)));

        // bản đồ: chọn chương đã mở (0-based)
        api.MapPost("/players/{id:guid}/map/{chapter:int}", (Guid id, int chapter, GameService game) =>
            Run(id, game, p => game.SelectMap(p, chapter)));

        // admin mode: lệnh gian lận để thử game (giao diện bật/tắt bằng Ctrl+Shift+A)
        api.MapPost("/players/{id:guid}/admin/{action}", (Guid id, string action, string? arg, int? n, GameService game) =>
            Run(id, game, p => game.AdminAction(p, action, arg, n ?? 1)));

        api.MapPost("/players/{id:guid}/reset", async (Guid id, GameService game) =>
        {
            try
            {
                var p = await game.LoadAsync(id);
                var fresh = await game.ResetAsync(p);
                return Results.Ok(new ActionResponse(await game.BuildStateAsync(fresh)));
            }
            catch (KeyNotFoundException) { return Results.NotFound(new { error = "Không tìm thấy tu sĩ." }); }
        });
    }

    // Nạp người chơi → thực hiện hành động → lưu DB → trả trạng thái mới
    private static Task<IResult> Run(Guid id, GameService game, Func<Player, (string? msg, string? kind)> action) =>
        RunCraft(id, game, p => { var (m, k) = action(p); return (m, k, null); });

    // Như Run, kèm kết quả luyện chế (thành phẩm + nguyên liệu) để giao diện hiện thông báo
    private static async Task<IResult> RunCraft(Guid id, GameService game, Func<Player, (string? msg, string? kind, CraftResultDto? craft)> action)
    {
        try
        {
            var p = await game.LoadAsync(id);
            var (msg, kind, craft) = action(p);
            await game.SaveAsync(p);
            return Results.Ok(new ActionResponse(await game.BuildStateAsync(p), msg, kind, craft));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = "Không tìm thấy tu sĩ." });
        }
        catch (GameException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
