using System.Globalization;
using VanDao.Data;

namespace VanDao.Game;

public partial class GameService
{
    public const int SpinDailyLimit = 100;
    private static readonly TimeSpan SpinDayOffset = TimeSpan.FromHours(7);
    public static SpinDailyDto SpinDailyFor(Player p, DateTimeOffset? now = null)
    {
        var local = (now ?? DateTimeOffset.UtcNow).ToOffset(SpinDayOffset);
        var reset = new DateTimeOffset(local.Date.AddDays(1), SpinDayOffset).ToUnixTimeMilliseconds();
        var day = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var used = p.SpinDay == day ? Math.Clamp(p.SpinDailyUsed, 0, SpinDailyLimit) : 0;
        // Save cũ: tính lượt hôm nay còn trong lịch sử, rồi lưu bộ đếm riêng từ lần quay kế tiếp.
        if (string.IsNullOrEmpty(p.SpinDay))
        {
            foreach (var entry in p.SpinHistory.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split('|');
                if (parts.Length != 2 || !long.TryParse(parts[0], out var seconds) || seconds < 0 || seconds > 253402300799) continue;
                if (seconds >= reset / 1000 - 86400 && seconds < reset / 1000)
                    used += parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            }
            used = Math.Min(SpinDailyLimit, used);
        }
        return new(SpinDailyLimit, used, SpinDailyLimit - used, reset);
    }
    private static SpinDailyDto CheckSpinDaily(Player p, int n, DateTimeOffset? now)
    {
        var quota = SpinDailyFor(p, now);
        if (n > quota.Remaining)
            throw new GameException($"Mỗi ngày chỉ được quay {SpinDailyLimit} lượt (dùng chung trang bị và linh thú). Còn {quota.Remaining} lượt; đặt lại lúc 00:00 giờ Việt Nam.");
        return quota;
    }
    private static void RecordSpinDaily(Player p, int n, SpinDailyDto quota)
    {
        p.SpinDay = DateTimeOffset.FromUnixTimeMilliseconds(quota.ResetAt).ToOffset(SpinDayOffset).AddDays(-1)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        p.SpinDailyUsed = quota.Used + n;
    }
}
