using System.Net.Http.Json;
using System.Text.Json;
using VanDao.Data;

namespace VanDao.Game;

/// <summary>Danh hiệu của người đứng hạng 1 một bảng xếp hạng (view titles_public).</summary>
public record TitleDto(string Id, string Name, string Desc);

public static class Titles
{
    // thứ tự = thứ tự hiện cạnh tên khi một người giữ nhiều danh hiệu
    public static readonly TitleDto[] All =
    [
        new("arena", "Lôi Đài Bá Chủ", "Đệ nhất Lôi đài"),
        new("power", "Chiến Lực Vô Song", "Đệ nhất bảng Lực chiến"),
        new("realm", "Cảnh Giới Chí Tôn", "Đệ nhất bảng Cảnh giới"),
        new("progress", "Phá Thiên Giả", "Đệ nhất bảng Vượt ải"),
        new("pet", "Vạn Thú Chi Vương", "Đệ nhất bảng Linh thú"),
    ];
    public static readonly Dictionary<string, TitleDto> ById = All.ToDictionary(t => t.Id);
}

public record ArenaRowDto(long Rank, Guid Id, string Name, int Realm, string RealmName, string StageName, long Power, int Wins, int Losses,
    bool Me, bool CanFight, List<TitleDto>? Titles);
public record ArenaHistoryDto(bool Attacker, string Other, bool Win, long From, long To, DateTime At);
public record ArenaDto(bool Enabled, string? Blocked, string? Error, ArenaRowDto? Me, List<ArenaRowDto> Top, List<ArenaRowDto> Targets,
    long Window, int Used, int Daily, long ReadyInMs, long Total, List<ArenaHistoryDto> History, bool Mock = false);
/// <summary>Một đòn trong trận: Mine = mình ra đòn. Máu là máu còn lại của hai bên sau đòn.</summary>
public record ArenaTurnDto(bool Mine, int Dmg, bool Crit, bool Dodge, int MyHp, int FoeHp);
public record ArenaFightDto(bool Win, bool Ko, string Me, int MyRealm, string Foe, int FoeRealm, long OldRank, long NewRank,
    int MaxMe, int MaxFoe, List<ArenaTurnDto> Turns);
public record ArenaFightResponse(StateDto State, string? Message, string? Kind, ArenaFightDto Fight);

/// <summary>
/// Lôi đài PK giữa người chơi trên Supabase (docs/arena/supabase.sql), dùng chung cấu hình "Leaderboard".
/// Supabase giữ bậc thang hạng và tự tính trận từ chỉ số đã gửi lên bảng xếp hạng; game chỉ hiển thị và ghi nhật ký.
/// </summary>
public sealed class ArenaService(LeaderboardOptions opt, ILogger<ArenaService> log, IConfiguration? cfg = null, IHostEnvironment? env = null)
{
    // Chỉ để hiển thị; luật thật nằm ở arena_rules() trong SQL — sửa thì sửa cả hai.
    public const int Daily = 15, CooldownSec = 20;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private readonly HttpClient _http = Client(opt);

    /// <summary>Lôi đài giả (ArenaMock): chỉ khi chạy Development và bật "Arena:Mock" (vd. dotnet run -- --Arena:Mock=true). Không gọi Supabase.</summary>
    public bool Mock { get; } = env?.IsDevelopment() == true && cfg?.GetValue<bool>("Arena:Mock") == true;
    private readonly Lazy<ArenaMock> _mock = new(() => new ArenaMock());
    public bool Enabled => opt.Enabled || Mock;

    public Task<StateRow> StateAsync(Player p) =>
        Mock ? Task.FromResult(_mock.Value.State(p)) : Rpc<StateRow>("arena_state", new { p_id = p.Id, p_secret = p.LbSecret });

    public Task<FightRow> FightAsync(Player p, Guid target) =>
        Mock ? Task.FromResult(_mock.Value.Fight(p, target)) : Rpc<FightRow>("arena_fight", new { p_id = p.Id, p_secret = p.LbSecret, p_target = target });

    /// <summary>Bản C# của arena_duel (docs/arena/supabase.sql), cùng LCG nên cùng seed ra cùng trận. Dùng cho Lôi đài giả.</summary>
    public static (bool win, bool ko, double maxA, double maxD, double hpA, double hpD, List<TurnRow> turns) Duel(CombatStats a, CombatStats d, long seed, int maxRounds)
    {
        double[] atk = [Math.Max(1, a.Atk), Math.Max(1, d.Atk)], def = [a.Def, d.Def], mhp = [Math.Max(1, a.Hp), Math.Max(1, d.Hp)];
        double[] crt = [Math.Min(100, a.Crit), Math.Min(100, d.Crit)], spd = [Math.Min(100, a.Speed), Math.Min(100, d.Speed)];
        double[] hp = [mhp[0], mhp[1]];
        var x = Math.Abs(seed) % 2147483648L;
        double Next() { x = (x * 1103515245L + 12345) % 2147483648L; return x / 2147483648.0; }
        var first = spd[1] > spd[0] ? 1 : 0;
        var turns = new List<TurnRow>(); var ko = false;
        for (var r = 0; r < maxRounds && !ko; r++)
            for (var i = 0; i < 2; i++)
            {
                var s = i == 0 ? first : 1 - first; var o = 1 - s;
                var dodge = Next() * 100 < Math.Min(spd[o], 50) / 2;
                var crit = !dodge && Next() * 100 < crt[s];
                if (dodge) Next();      // SQL luôn rút 2 số mỗi đòn
                var dmg = dodge ? 0 : Math.Max(1, Math.Round(atk[s] * atk[s] / (atk[s] + def[o]) * (crit ? 1.5 : 1), MidpointRounding.AwayFromZero));
                hp[o] = Math.Max(0, hp[o] - dmg);
                turns.Add(new TurnRow(s == 0 ? "a" : "d", dmg, crit, dodge, hp[0], hp[1]));
                if (hp[o] <= 0) { ko = true; break; }
            }
        var win = hp[1] <= 0 || (hp[0] > 0 && hp[0] / mhp[0] > hp[1] / mhp[1]);
        return (win, ko, mhp[0], mhp[1], hp[0], hp[1], turns);
    }

    private static HttpClient Client(LeaderboardOptions o)
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        if (!o.Enabled) return h;
        h.BaseAddress = new Uri(o.Url.TrimEnd('/') + "/rest/v1/");
        h.DefaultRequestHeaders.Add("apikey", o.AnonKey);
        h.DefaultRequestHeaders.Add("Authorization", $"Bearer {o.AnonKey}");
        h.DefaultRequestHeaders.Add("X-Game-Version", AppVersion.Current);   // Supabase chặn bản cũ (require_version)
        return h;
    }

    public sealed record Row(Guid PlayerId, string Name, int Realm, int Stage, string RealmName, string StageName, long Power, int Wins, int Losses, long Rank);
    public sealed record HistoryRow(long Id, bool Attacker, string OtherName, bool Win, long FromRank, long ToRank, DateTime CreatedAt);
    public sealed record StateRow(Row? Me, List<Row> Top, List<Row> Targets, long Window, int Used, int Daily, DateTime? ReadyAt, long Total, List<HistoryRow> History);
    public sealed record TurnRow(string By, double Dmg, bool Crit, bool Dodge, double HpA, double HpD);
    public sealed record FightRow(bool Win, bool Ko, double MaxA, double MaxD, double HpA, double HpD, List<TurnRow> Turns,
        long Id, string FoeName, int FoeRealm, long OldRank, long NewRank, long FoeRank);

    /// <summary>Gọi hàm SQL; lỗi do luật Lôi đài (RAISE EXCEPTION) trả về đúng câu tiếng Việt cho người chơi.</summary>
    public async Task<T> Rpc<T>(string fn, object body)
    {
        HttpResponseMessage res;
        try { res = await _http.PostAsync($"rpc/{fn}", new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.LogWarning("Arena {Fn}: {Error}", fn, ex.Message);
            throw new GameException("Không kết nối được Lôi đài. Kiểm tra mạng rồi thử lại.");
        }
        using (res)
        {
            var text = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                string? msg = null;
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    var code = doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
                    if (code == "P0001") msg = doc.RootElement.GetProperty("message").GetString();
                    else if (code is "PGRST202" or "42883") msg = "Cần chạy docs/arena/supabase.sql trên Supabase để mở Lôi đài.";
                }
                catch (JsonException) { }
                if (msg is null) log.LogWarning("Arena {Fn}: {Status} {Body}", fn, (int)res.StatusCode, text);
                throw new GameException(msg ?? "Lôi đài đang gặp lỗi, thử lại sau.");
            }
            try { return JsonSerializer.Deserialize<T>(text, Json)!; }
            catch (JsonException ex)
            {
                log.LogWarning("Arena {Fn}: {Error} {Body}", fn, ex.Message, text);
                throw new GameException("Lôi đài trả dữ liệu lạ, thử lại sau.");
            }
        }
    }
}
