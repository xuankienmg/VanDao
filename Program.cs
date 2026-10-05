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
            scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.EnsureCreated();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        MapApi(app);
        return app;
    }

    private static void MapApi(WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/data", () => new GameDataDto(
            GameData.Items,
            GameData.Realms.Select((r, i) => new RealmInfoDto(i, r.Id, r.Name, r.Desc, r.Stages, r.Aura, r.Lotus, r.Sash, r.Resources)),
            GameData.Recipes.Select((r, i) => new RecipeInfoDto(i, r.Name, r.OutItem, r.OutCount, r.In, r.Rate, r.MinRealm, GameData.Realms[r.MinRealm].Name)),
            GameData.LockedRealms));

        api.MapPost("/players", async (CreatePlayerRequest? req, GameService game) =>
        {
            var p = await game.CreateAsync(req?.Name);
            return Results.Ok(await game.BuildStateAsync(p));
        });

        api.MapGet("/players/{id:guid}", (Guid id, GameService game) =>
            Run(id, game, _ => (null, null)));

        api.MapPost("/players/{id:guid}/channel", (Guid id, GameService game) =>
            Run(id, game, p => (game.Channel(p), null)));

        api.MapPost("/players/{id:guid}/explore", (Guid id, GameService game) =>
            Run(id, game, p => game.Explore(p)));

        api.MapPost("/players/{id:guid}/advance", (Guid id, GameService game) =>
            Run(id, game, p => game.Advance(p)));

        api.MapPost("/players/{id:guid}/craft/{index:int}", (Guid id, int index, GameService game) =>
            Run(id, game, p => game.Craft(p, index)));

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
    private static async Task<IResult> Run(Guid id, GameService game, Func<Player, (string? msg, string? kind)> action)
    {
        try
        {
            var p = await game.LoadAsync(id);
            var (msg, kind) = action(p);
            await game.SaveAsync(p);
            return Results.Ok(new ActionResponse(await game.BuildStateAsync(p), msg, kind));
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
