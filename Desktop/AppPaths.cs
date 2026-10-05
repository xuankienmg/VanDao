namespace VanDao.Desktop;

/// <summary>Thư mục dữ liệu của game: %LocalAppData%\VanDao (DB, cài đặt cửa sổ, cache WebView2).</summary>
public static class AppPaths
{
    public static string DataDir { get; } = Ensure(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VanDao"));

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
