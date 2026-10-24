using VanDao.Data;

namespace VanDao.Game;

/// <summary>Lôi đài: gửi chỉ số mới nhất lên bảng xếp hạng, rồi để Supabase (ArenaService) tính trận và đổi hạng.</summary>
public partial class GameService
{
    private string? ArenaBlocked(Player p) =>
        lb is not null && lb.Excluded(p) ? "Nhân vật đã dùng admin mode nên không vào được Lôi đài. Tu lại từ đầu (không bật cờ admin) để tham gia."
        : p.LbSecret.Length == 0 ? "Nhân vật chưa có khóa bảng xếp hạng."
        : null;

    private ArenaService Arena(Player p)
    {
        if (_arena is not { Enabled: true } || lb is null) throw new GameException("Lôi đài chưa được cấu hình (appsettings.json → Leaderboard).");
        if (ArenaBlocked(p) is { } why) throw new GameException(why);
        return _arena;
    }

    public async Task<ArenaDto> ArenaAsync(Player p)
    {
        var empty = new ArenaDto(_arena is { Enabled: true } && lb is not null, ArenaBlocked(p), null, null, [], [], 0, 0, ArenaService.Daily, 0, 0, [], _arena?.Mock == true);
        if (!empty.Enabled || empty.Blocked is not null) return empty;
        try
        {
            var a = Arena(p);
            if (!a.Mock) await lb!.SubmitNowAsync(p);     // lực chiến / cảnh giới trên Lôi đài khớp nhân vật hiện tại
            var s = await a.StateAsync(p);
            var titles = a.Mock ? MockTitles(s) : await lb!.TitlesAsync();
            var myRank = s.Me?.Rank ?? long.MaxValue;
            ArenaRowDto Dto(ArenaService.Row x) => new(x.Rank, x.PlayerId, x.Name, x.Realm, x.RealmName, x.StageName, x.Power, x.Wins, x.Losses,
                x.PlayerId == p.Id, x.Rank < myRank && myRank - x.Rank <= s.Window, titles.GetValueOrDefault(x.PlayerId));
            var readyIn = s.ReadyAt is { } at ? Math.Max(0, (long)(at.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds) : 0;
            return empty with
            {
                Me = s.Me is null ? null : Dto(s.Me), Top = s.Top.Select(Dto).ToList(), Targets = s.Targets.Select(Dto).ToList(),
                Window = s.Window, Used = s.Used, Daily = s.Daily, ReadyInMs = readyIn, Total = s.Total,
                History = s.History.Select(h => new ArenaHistoryDto(h.Attacker, h.OtherName, h.Win, h.FromRank, h.ToRank, DateTime.SpecifyKind(h.CreatedAt.ToUniversalTime(), DateTimeKind.Utc))).ToList(),
            };
        }
        catch (GameException ex) { return empty with { Error = ex.Message }; }
    }

    /// <summary>Lôi đài giả: chỉ có danh hiệu Lôi Đài Bá Chủ cho hạng 1 giả (không đọc danh hiệu thật trên Supabase).</summary>
    private static Dictionary<Guid, List<TitleDto>> MockTitles(ArenaService.StateRow s) =>
        s.Top.FirstOrDefault(x => x.Rank == 1) is { } top ? new() { [top.PlayerId] = [Titles.ById["arena"]] } : new();

    public async Task<(string msg, string kind, ArenaFightDto fight)> ArenaFightAsync(Player p, Guid target)
    {
        var a = Arena(p);
        // Supabase tính trận từ chỉ số trên bảng xếp hạng: gửi bản mới nhất trước (trang bị vừa thay vẫn được tính)
        if (!a.Mock && !await lb!.SubmitNowAsync(p)) throw new GameException("Không gửi được chỉ số lên bảng xếp hạng. Kiểm tra mạng rồi thử lại.");
        var f = await a.FightAsync(p, target);
        lb?.ForgetTitles();
        static int I(double v) => (int)Math.Min(int.MaxValue, Math.Round(v));
        var fight = new ArenaFightDto(f.Win, f.Ko, p.Name, p.Realm, f.FoeName, f.FoeRealm, f.OldRank, f.NewRank, I(f.MaxA), I(f.MaxD),
            f.Turns.Select(t => new ArenaTurnDto(t.By == "a", I(t.Dmg), t.Crit, t.Dodge, I(t.HpA), I(t.HpD))).ToList());
        var (msg, kind) = f.Win
            ? Log(p, f.NewRank < f.OldRank
                ? $"Lôi đài: đánh bại {f.FoeName}, chiếm hạng #{f.NewRank}{(f.NewRank == 1 ? " — Lôi Đài Bá Chủ!" : "")}."
                : $"Lôi đài: đánh bại {f.FoeName}.", f.NewRank == 1 ? "gold" : "good")
            : Log(p, $"Lôi đài: bại dưới tay {f.FoeName}, giữ hạng #{f.OldRank}.", "bad");
        return (msg, kind, fight);
    }
}
