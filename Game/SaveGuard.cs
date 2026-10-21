using Microsoft.EntityFrameworkCore;
using VanDao.Data;

namespace VanDao.Game;

/// <summary>
/// Chống quay về bản cũ (downgrade): file vandao.db ghi lại phiên bản game cao nhất từng mở nó (bảng "Meta", khóa max_version).
/// Bản game cũ hơn con số đó thì từ chối mọi thao tác với nhân vật (kể cả khi offline) và hiện màn hình yêu cầu tải bản mới,
/// để không thể chơi bản cũ còn lỗi (vd. nhân đôi tài nguyên) rồi mang kết quả sang bản mới.
/// Môi trường dev (dotnet run) không kiểm tra và không ghi dấu, để thử bản đang làm không khóa bản phát hành.
/// Chỉ có tác dụng với các bản phát hành từ khi có SaveGuard; bản cũ hơn nữa không biết đọc dấu này (đã bị chặn online bằng min_version).
/// </summary>
public static class SaveGuard
{
    private const string Key = "max_version";

    /// <summary>Phiên bản đã dùng file dữ liệu này khi nó mới hơn bản đang chạy; null = không chặn.</summary>
    public static string? NewerSave { get; private set; }
    /// <summary>Số nhân vật vừa bị đánh dấu ở lần khởi động này vì file từng bị bản cũ ghi vào (0 = không).</summary>
    public static int OldBuildRows { get; private set; }

    public static void Check(GameDbContext db, bool dev)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """CREATE TABLE IF NOT EXISTS "Meta" ("Key" TEXT NOT NULL PRIMARY KEY, "Value" TEXT NOT NULL);""";
        cmd.ExecuteNonQuery();
        cmd.CommandText = """SELECT "Value" FROM "Meta" WHERE "Key" = $k""";
        var p = cmd.CreateParameter(); p.ParameterName = "$k"; p.Value = Key; cmd.Parameters.Add(p);
        var stored = cmd.ExecuteScalar() as string;
        // bản không có SaveGuard (vd. 0.5.2) đã ghi nhật ký sau lần lưu cuối của bản mới → đánh dấu mọi nhân vật trong file
        cmd.CommandText = """SELECT "Value" FROM "Meta" WHERE "Key" = 'log_mark'""";
        var mark = cmd.ExecuteScalar() as string;
        cmd.CommandText = """SELECT COALESCE(MAX("Id"), 0) FROM "Logs" """;
        var maxLog = Convert.ToInt64(cmd.ExecuteScalar());
        if (!dev && long.TryParse(mark, out var m) && maxLog > m)
        {
            cmd.CommandText = """UPDATE "Players" SET "Flagged" = 1""";
            OldBuildRows = cmd.ExecuteNonQuery();
        }
        cmd.CommandText = $"""INSERT INTO "Meta" ("Key", "Value") VALUES ('log_mark', '{maxLog}') ON CONFLICT("Key") DO UPDATE SET "Value" = excluded."Value";""";
        cmd.ExecuteNonQuery();
        if (dev) return;
        var cur = AppVersion.Parse(AppVersion.Current);
        if (cur == new Version(0, 0, 0)) return;                       // không đọc được phiên bản bản build: không chặn, không ghi
        if (stored is not null && AppVersion.Parse(stored) > cur) { NewerSave = stored; return; }
        if (stored is not null && AppVersion.Parse(stored) == cur) return;
        cmd.CommandText = """INSERT INTO "Meta" ("Key", "Value") VALUES ($k, $v) ON CONFLICT("Key") DO UPDATE SET "Value" = excluded."Value";""";
        var v = cmd.CreateParameter(); v.ParameterName = "$v"; v.Value = AppVersion.Current; cmd.Parameters.Add(v);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Gộp vào kết quả kiểm tra phiên bản: file dữ liệu mới hơn bản đang chạy thì bắt buộc cập nhật.</summary>
    public static VersionDto Apply(VersionDto v) => NewerSave is null ? v : v with
    {
        Latest = NewerSave, Min = NewerSave, Status = "required", Blocked = true,
        Message = $"Dữ liệu tu luyện trên máy này đã được bản {NewerSave} sử dụng nên không mở bằng bản {v.Current} cũ hơn. Hãy chạy bản {NewerSave} trở lên.",
    };
}
