using System.Text.Json;
using System.Text.Json.Serialization;

namespace VanDao.Desktop;

/// <summary>Vị trí neo của dải game so với thanh taskbar (giống "Screen Layout" của Task Bar Hero).</summary>
public enum StripDock { Auto, BottomLeft, BottomCenter, BottomRight, TopLeft, TopCenter, TopRight, Manual }

/// <summary>Cài đặt cửa sổ, lưu ở %LocalAppData%\VanDao\window.json.</summary>
public class WindowSettings
{
    public StripDock Dock { get; set; } = StripDock.Auto;

    /// <summary>Toạ độ khi người dùng tự kéo (Dock = Manual).</summary>
    public int? X { get; set; }
    public int? Y { get; set; }

    /// <summary>Màn hình đặt dải game (DeviceName). null = màn hình chính.</summary>
    public string? Screen { get; set; }

    /// <summary>Tỉ lệ kích thước dải game, 1 = 280×200.</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Luôn hiển thị trên các cửa sổ khác.</summary>
    public bool Pinned { get; set; } = true;

    /// <summary>Nền trong suốt: chỉ thấy nhân vật và bảng thông tin, bấm xuyên qua phần trống.</summary>
    public bool Transparent { get; set; } = true;

    public double Opacity { get; set; } = 1.0;

    public int? PanelWidth { get; set; }
    public int? PanelHeight { get; set; }

    private static string FilePath => Path.Combine(AppPaths.DataDir, "window.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static WindowSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(FilePath), Json) ?? new();
        }
        catch { /* file hỏng hoặc từ bản cũ thì dùng mặc định */ }
        return new();
    }

    public void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json)); }
        catch { /* không lưu được cũng không chặn game */ }
    }
}
