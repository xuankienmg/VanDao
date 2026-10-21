using System.Text.Json;
using System.Text.RegularExpressions;

namespace VanDao.Launcher;

public class LauncherSettings
{
    public string GamePath { get; set; } = "";
    public string GitHubRepo { get; set; } = "xuankienmg/VanDao";
    public bool AutoCheckOnStartup { get; set; } = true;
    public bool CloseLauncherOnPlay { get; set; } = true;
    public string CurrentVersion { get; set; } = "0.7.0";
    public string LauncherVersion { get; set; } = "1.0.0";
    public string Theme { get; set; } = "dark";

    public static string GetSettingsFile()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var inSubDir = Path.Combine(baseDir, "VanDaoLauncher", "launcher_settings.json");
        var inBaseDir = Path.Combine(baseDir, "launcher_settings.json");

        if (File.Exists(inSubDir)) return inSubDir;
        if (Directory.Exists(Path.Combine(baseDir, "VanDaoLauncher"))) return inSubDir;
        if (File.Exists(inBaseDir)) return inBaseDir;

        return inSubDir;
    }

    public static LauncherSettings Load()
    {
        var settingsFile = GetSettingsFile();
        try
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var s = JsonSerializer.Deserialize<LauncherSettings>(json);
                if (s != null)
                {
                    if (string.IsNullOrEmpty(s.GamePath))
                        s.GamePath = DetectGamePath();
                    if (string.IsNullOrEmpty(s.LauncherVersion))
                        s.LauncherVersion = "1.0.0";

                    // Luôn tự động đồng bộ phiên bản từ file exe NguoiLuoiTuTien.exe trên máy
                    var detected = DetectGameVersion(s.GamePath);
                    if (!string.IsNullOrEmpty(detected))
                    {
                        s.CurrentVersion = detected;
                        s.Save();
                    }

                    // Rewrite legacy settings without any stored GitHub token.
                    s.Save();
                    return s;
                }
            }
        }
        catch { }

        var settings = new LauncherSettings { GamePath = DetectGamePath() };
        settings.CurrentVersion = DetectGameVersion(settings.GamePath);
        settings.Save();
        return settings;
    }

    public void Save()
    {
        try
        {
            var settingsFile = GetSettingsFile();
            var dir = Path.GetDirectoryName(settingsFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }
        catch { }
    }

    public static string DetectGameVersion(string gamePath)
    {
        try
        {
            // 1. Kiểm tra trực tiếp file exe NguoiLuoiTuTien.exe (chính xác nhất cho người chơi)
            var exeCandidates = new[]
            {
                Path.Combine(gamePath, "NguoiLuoiTuTien.exe"),
                Path.Combine(gamePath, "bin", "Release", "net8.0-windows", "NguoiLuoiTuTien.exe"),
                Path.Combine(gamePath, "bin", "Debug", "net8.0-windows", "NguoiLuoiTuTien.exe")
            };

            foreach (var exe in exeCandidates)
            {
                if (File.Exists(exe))
                {
                    var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(exe);
                    if (!string.IsNullOrWhiteSpace(vi.ProductVersion))
                    {
                        var clean = vi.ProductVersion.Split('+')[0].Trim();
                        if (!string.IsNullOrEmpty(clean)) return clean;
                    }
                    if (!string.IsNullOrWhiteSpace(vi.FileVersion))
                    {
                        var parts = vi.FileVersion.Split('.');
                        if (parts.Length >= 3)
                        {
                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
                        }
                        return vi.FileVersion.Trim();
                    }
                }
            }

            // 2. Fallback kiểm tra VanDao.csproj nếu ở môi trường dev
            var csproj = Path.Combine(gamePath, "VanDao.csproj");
            if (File.Exists(csproj))
            {
                var content = File.ReadAllText(csproj);
                var match = Regex.Match(content, @"<Version>(.*?)</Version>");
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        catch { }
        return "0.7.2";
    }

    public static string DetectGamePath()
    {
        // 1. Tìm ngược lên từ thư mục chạy của Launcher
        var current = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(current);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NguoiLuoiTuTien.exe")) ||
                File.Exists(Path.Combine(dir.FullName, "VanDao.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        // 2. Dự phòng các đường dẫn mặc định
        var candidates = new[]
        {
            @"D:\Game VanDao",
            @"C:\Game VanDao",
            Path.Combine(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory) ?? @"D:\", "Game VanDao")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(Path.Combine(path, "NguoiLuoiTuTien.exe")) ||
                File.Exists(Path.Combine(path, "VanDao.csproj")))
                return path;
        }

        return @"D:\Game VanDao";
    }
}
