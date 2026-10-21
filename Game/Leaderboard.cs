using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using VanDao.Data;

namespace VanDao.Game;

/// <summary>Cấu hình "Leaderboard" trong appsettings.json: URL project Supabase + anon key. Bỏ trống = tắt bảng xếp hạng.</summary>
public sealed class LeaderboardOptions
{
    public string Url { get; set; } = "";
    public string AnonKey { get; set; } = "";
    public bool Enabled => Url.Length > 0 && AnonKey.Length > 0;
}

public record LbRowDto(long Rank, string Name, int Realm, int Stage, string RealmName, string StageName, long Power, bool Me, Guid Id = default, bool Flagged = false);
/// <summary>Hồ sơ một người trên bảng: trang bị đang mặc (ô → mã vật phẩm) và chỉ số chiến đấu lúc gửi điểm gần nhất.</summary>
public record LbProfileDto(string Name, int Realm, int Stage, string RealmName, string StageName, long Power,
    Dictionary<string, string> Equipment, Dictionary<string, double> Stats, DateTime UpdatedAt, bool Me);
public record LeaderboardDto(bool Enabled, string By, List<LbRowDto> Rows, LbRowDto? Me, bool Tainted, string Name, string? Error = null, bool Flagged = false);

/// <summary>
/// Bảng xếp hạng online (Supabase, xem docs/leaderboard/supabase.sql).
/// Gửi điểm chạy nền, có giới hạn tần suất; lỗi mạng không bao giờ làm hỏng lượt chơi.
/// </summary>
public sealed class LeaderboardService(LeaderboardOptions opt, ILogger<LeaderboardService> log, IHostEnvironment? env = null)
{
    /// <summary>Môi trường dev (ASPNETCORE_ENVIRONMENT=Development): bỏ qua cờ admin (Tainted), nhân vật thử nghiệm vẫn lên bảng.</summary>
    private bool Dev => env?.IsDevelopment() == true;
    private bool Excluded(Player p) => p.Tainted && !Dev;

    public const int Top = 50;
    private static readonly TimeSpan MinGap = TimeSpan.FromSeconds(15);     // đổi cảnh giới / lực chiến / tên: gửi sớm nhất sau 15 giây
    private static readonly TimeSpan Refresh = TimeSpan.FromMinutes(3);     // không đổi gì: vẫn cập nhật tu vi mỗi 3 phút
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private static readonly string Version = typeof(LeaderboardService).Assembly.GetName().Version?.ToString(3) ?? "";

    private readonly HttpClient _http = CreateClient(opt);
    private readonly ConcurrentDictionary<Guid, (DateTime at, string key)> _sent = new();
    private readonly ConcurrentDictionary<string, (DateTime at, List<Row> rows)> _cache = new();

    public bool Enabled => opt.Enabled;

    private static HttpClient CreateClient(LeaderboardOptions o)
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        if (!o.Enabled) return h;
        h.BaseAddress = new Uri(o.Url.TrimEnd('/') + "/rest/v1/");
        h.DefaultRequestHeaders.Add("apikey", o.AnonKey);
        h.DefaultRequestHeaders.Add("Authorization", $"Bearer {o.AnonKey}");
        h.DefaultRequestHeaders.Add("X-Game-Version", AppVersion.Current);   // Supabase chặn bản cũ (require_version)
        return h;
    }

    /// <summary>Gọi sau mỗi lần lưu nhân vật: gửi điểm nếu đến lúc (chạy nền, không chờ).</summary>
    public void Track(Player p, bool force = false)
    {
        if (!Enabled || p.LbSecret.Length == 0) return;
        if (Excluded(p))
        {
            if (_sent.TryGetValue(p.Id, out var s) && s.key == "removed") return;
            _sent[p.Id] = (DateTime.UtcNow, "removed");
            var (rid, rsec) = (p.Id, p.LbSecret);
            _ = Task.Run(() => Post("rpc/remove_score", new { p_id = rid, p_secret = rsec }));
            return;
        }
        var r = GameService.Realm(p);
        var power = GameService.Power(p);
        var equipment = p.Equipment.Where(e => e.ItemId.Length > 0).OrderBy(e => e.Slot).ToDictionary(e => e.Slot, e => e.ItemId);
        var st = GameService.Stats(p);
        var key = $"{p.Name}|{p.Realm}|{p.Stage}|{power}|{string.Join(',', equipment.Values)}";   // đổi trang bị cũng gửi sớm
        var now = DateTime.UtcNow;
        if (!force && _sent.TryGetValue(p.Id, out var last) && now - last.at < (last.key == key ? Refresh : MinGap)) return;
        _sent[p.Id] = (now, key);
        var body = new
        {
            p_id = p.Id, p_secret = p.LbSecret, p_name = p.Name, p_realm = p.Realm, p_stage = p.Stage,
            p_level = GameService.Level(p), p_qi = Math.Round(p.Qi, 1), p_realm_name = r.Name, p_stage_name = r.Stages[p.Stage],
            p_power = power, p_version = Version, p_equipment = equipment, p_flagged = p.Flagged,
            p_stats = new Dictionary<string, double> { ["atk"] = st.Atk, ["def"] = st.Def, ["hp"] = st.Hp, ["crit"] = Math.Round(st.Crit, 1), ["speed"] = Math.Round(st.Speed, 1) },
        };
        _ = Task.Run(async () =>
        {
            if (!await Post("rpc/submit_score", body, quietNotFound: true) && _oldSchema)
                await Post("rpc/submit_score", new { body.p_id, body.p_secret, body.p_name, body.p_realm, body.p_stage, body.p_level, body.p_qi, body.p_realm_name, body.p_stage_name, body.p_power, body.p_version, body.p_equipment, body.p_stats });
            _cache.Clear();                     // để người chơi thấy ngay hạng mới của mình
        });
    }

    /// <summary>Supabase chưa chạy docs/leaderboard/supabase.sql bản có cột flagged: đọc / gửi theo kiểu cũ (không có dấu *).</summary>
    private volatile bool _oldSchema;

    private async Task<bool> Post(string path, object body, bool quietNotFound = false)
    {
        try
        {
            using var res = await _http.PostAsync(path, new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode) return true;
            var text = await res.Content.ReadAsStringAsync();
            if (quietNotFound && text.Contains("p_flagged")) { _oldSchema = true; return false; }
            log.LogWarning("Leaderboard {Path}: {Status} {Body}", path, (int)res.StatusCode, text);
        }
        catch (Exception ex) { log.LogWarning("Leaderboard {Path}: {Error}", path, ex.Message); }
        return false;
    }

    private sealed record Row(Guid PlayerId, string Name, int Realm, int Stage, string RealmName, string StageName, long Power, long RealmRank, long PowerRank, bool Flagged = false);

    private const string Cols = "player_id,name,realm,stage,realm_name,stage_name,power,realm_rank,power_rank,flagged";
    private const string OldCols = "player_id,name,realm,stage,realm_name,stage_name,power,realm_rank,power_rank";

    /// <summary>Đọc view leaderboard_public; view chưa có cột flagged (chưa chạy SQL mới) thì đọc kiểu cũ.</summary>
    private async Task<List<Row>> ReadRows(string query)
    {
        if (!_oldSchema)
        {
            using var res = await _http.GetAsync($"leaderboard_public?select={Cols}&{query}");
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<List<Row>>(Json) ?? [];
            if (!(await res.Content.ReadAsStringAsync()).Contains("flagged")) res.EnsureSuccessStatusCode();
            _oldSchema = true;
        }
        return await _http.GetFromJsonAsync<List<Row>>($"leaderboard_public?select={OldCols}&{query}", Json) ?? [];
    }

    /// <param name="by">"realm" (cảnh giới) hoặc "power" (lực chiến).</param>
    public async Task<LeaderboardDto> GetAsync(Player p, string? by)
    {
        by = by == "power" ? "power" : "realm";
        if (!Enabled) return new LeaderboardDto(false, by, [], null, Excluded(p), p.Name);
        try
        {
            if (!_cache.TryGetValue(by, out var c) || DateTime.UtcNow - c.at > CacheFor)
            {
                var order = by == "power" ? "power_rank.asc,realm_rank.asc" : "realm_rank.asc,power_rank.asc";
                c = (DateTime.UtcNow, await ReadRows($"order={order}&limit={Top}"));
                _cache[by] = c;
            }
            var mine = c.rows.FirstOrDefault(x => x.PlayerId == p.Id)
                       ?? (await ReadRows($"player_id=eq.{p.Id}")).FirstOrDefault();
            LbRowDto Dto(Row x) => new(by == "power" ? x.PowerRank : x.RealmRank, x.Name, x.Realm, x.Stage, x.RealmName, x.StageName, x.Power, x.PlayerId == p.Id, x.PlayerId, x.Flagged);
            return new LeaderboardDto(true, by, c.rows.Select(Dto).ToList(), Excluded(p) || mine is null ? null : Dto(mine), Excluded(p), p.Name, Flagged: p.Flagged);
        }
        catch (Exception ex)
        {
            log.LogWarning("Leaderboard get: {Error}", ex.Message);
            return new LeaderboardDto(true, by, [], null, Excluded(p), p.Name, "Không kết nối được bảng xếp hạng. Kiểm tra mạng rồi thử lại.");
        }
    }

    private sealed record ProfileRow(Guid PlayerId, string Name, int Realm, int Stage, string RealmName, string StageName, long Power,
        Dictionary<string, string>? Equipment, Dictionary<string, double>? Stats, DateTime UpdatedAt);
    private readonly ConcurrentDictionary<Guid, (DateTime at, ProfileRow row)> _profiles = new();

    /// <summary>Trang bị + chỉ số của một người trên bảng (bấm vào dòng). Null = không có trên bảng.</summary>
    public async Task<LbProfileDto?> ProfileAsync(Player me, Guid playerId)
    {
        if (!Enabled) throw new GameException("Bảng xếp hạng online chưa được cấu hình.");
        if (!_profiles.TryGetValue(playerId, out var c) || DateTime.UtcNow - c.at > CacheFor)
        {
            List<ProfileRow>? rows;
            try { rows = await _http.GetFromJsonAsync<List<ProfileRow>>($"leaderboard_public?select=player_id,name,realm,stage,realm_name,stage_name,power,equipment,stats,updated_at&player_id=eq.{playerId}", Json); }
            catch (Exception ex) { log.LogWarning("Leaderboard profile: {Error}", ex.Message); throw new GameException("Không kết nối được bảng xếp hạng. Kiểm tra mạng rồi thử lại."); }
            if (rows is not { Count: > 0 }) return null;
            c = (DateTime.UtcNow, rows[0]); _profiles[playerId] = c;
        }
        var r = c.row;
        // trang bị bản game khác (đã đổi mã, vd. Thương → Kiếm) quy về mã hiện tại; món lạ giữ nguyên để giao diện hiện "?"
        var eq = (r.Equipment ?? new()).ToDictionary(kv => kv.Key, kv => GameData.ReplaceSpear(kv.Value) ?? GameData.UpgradeGear(kv.Value) ?? kv.Value);
        return new LbProfileDto(r.Name, r.Realm, r.Stage, r.RealmName, r.StageName, r.Power, eq, r.Stats ?? new(), DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc), r.PlayerId == me.Id);
    }
}
