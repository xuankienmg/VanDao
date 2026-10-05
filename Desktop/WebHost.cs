using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace VanDao.Desktop;

/// <summary>Dùng chung một môi trường WebView2 để dải game và bảng chia sẻ cùng tu sĩ (localStorage).</summary>
internal static class WebHost
{
    private static Task<CoreWebView2Environment>? _env;

    public static Task<CoreWebView2Environment> Environment =>
        _env ??= CoreWebView2Environment.CreateAsync(null, Path.Combine(AppPaths.DataDir, "WebView2"));

    /// <summary>Khởi tạo WebView2, nhận tin nhắn {type: ...} từ trang và mở link ngoài bằng trình duyệt.</summary>
    public static async Task InitAsync(WebView2 web, Action<string, JsonElement> onMessage)
    {
        await web.EnsureCoreWebView2Async(await Environment);
        var s = web.CoreWebView2.Settings;
        s.IsStatusBarEnabled = false;
        s.IsZoomControlEnabled = false;
        s.AreBrowserAcceleratorKeysEnabled = false;
        web.CoreWebView2.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
        };
        web.CoreWebView2.WebMessageReceived += (_, e) =>
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var t)) onMessage(t.GetString() ?? "", root.Clone());
            }
            catch (JsonException) { }
        };
    }

    public static Task SyncAsync(WebView2 web) =>
        web.CoreWebView2 is null ? Task.CompletedTask : web.ExecuteScriptAsync("window.sync && window.sync()");
}
