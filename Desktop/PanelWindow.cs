using Microsoft.Web.WebView2.WinForms;

namespace VanDao.Desktop;

/// <summary>
/// Cửa sổ Tu luyện (Đột phá / Hành trang / Luyện đan) bật lên sát dải game khi cần,
/// đóng lại thì game quay về dạng dải gọn trên taskbar.
/// </summary>
public sealed class PanelWindow : Form
{
    private static readonly Dictionary<string, string> TabNames = new()
    {
        ["req"] = "Đột phá", ["inv"] = "Hành trang", ["craft"] = "Luyện đan",
    };

    private readonly GameWindow _strip;
    private readonly WindowSettings _s;
    private readonly string _url;
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Theme.Ink };
    private bool _ready;
    private string _tab = "req";

    public PanelWindow(GameWindow strip, WindowSettings settings, string url)
    {
        _strip = strip;
        _s = settings;
        _url = url;

        Owner = strip; // luôn nổi trên dải game, ẩn/hiện cùng dải
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Ink;
        KeyPreview = true;
        MinimumSize = LogicalToDeviceUnits(new Size(260, 300));
        Size = SizeFor(_tab);
        Controls.Add(_web);

        Load += async (_, _) =>
        {
            await WebHost.InitAsync(_web, OnMessage);
            _web.CoreWebView2.NavigationCompleted += (_, _) => _ready = true;
            _web.Source = new Uri($"{_url}?mode=panel&tab={_tab}");
        };
        ResizeEnd += (_, _) =>
        {
            if (_tab == "inv") { _s.InvW = Width; _s.InvH = Height; }
            else { _s.PanelW = Width; _s.PanelH = Height; }
            _s.Save();
        };
        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true; // giữ lại để mở lần sau cho nhanh
            Hide();
        };
    }

    public async void ShowTab(string tab)
    {
        _tab = TabNames.ContainsKey(tab) ? tab : "req";
        Text = $"Tu luyện · {TabNames[_tab]}";
        TopMost = _s.Pinned;
        Size = SizeFor(_tab); // Hành trang rộng, Đột phá / Luyện đan gọn
        PlaceNearStrip();
        if (!Visible) Show();
        Activate();
        if (_ready)
        {
            await _web.ExecuteScriptAsync($"window.openTab && window.openTab('{_tab}')");
            await WebHost.SyncAsync(_web);
        }
    }

    /// <summary>Kích thước theo tab: dùng kích thước người dùng đã kéo, nếu chưa có thì mặc định.</summary>
    private Size SizeFor(string tab) => tab == "inv"
        ? (_s.InvW is int iw && _s.InvH is int ih ? new Size(iw, ih) : LogicalToDeviceUnits(new Size(740, 680)))
        : (_s.PanelW is int w && _s.PanelH is int h ? new Size(w, h) : LogicalToDeviceUnits(new Size(310, 430)));

    public Task SyncAsync() => Visible ? WebHost.SyncAsync(_web) : Task.CompletedTask;

    /// <summary>Đặt bảng ngay phía trên dải (hoặc phía dưới nếu dải nằm ở mép trên màn hình).</summary>
    public void PlaceNearStrip()
    {
        var area = Screen.FromControl(_strip).WorkingArea;
        var gap = LogicalToDeviceUnits(6);
        var s = _strip.Bounds;
        var above = s.Top - area.Top >= area.Bottom - s.Bottom;
        var y = above ? s.Top - Height - gap : s.Bottom + gap;
        var x = s.Right - Width;
        Location = new Point(
            Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height)));
    }

    private void OnMessage(string type, System.Text.Json.JsonElement msg)
    {
        switch (type)
        {
            case "changed": _ = _strip.SyncAsync(); break;
            case "close-panel": Hide(); break;
            case "tab" when msg.TryGetProperty("tab", out var t) && TabNames.ContainsKey(t.GetString() ?? ""):
                var tab = t.GetString()!;
                if (tab == _tab) break;
                _tab = tab;
                Text = $"Tu luyện · {TabNames[_tab]}";
                var size = SizeFor(_tab);
                if (size != Size) { Size = size; PlaceNearStrip(); }
                break;
            case "break": _strip.PlayBreak(msg.TryGetProperty("kind", out var k) ? k.GetString() ?? "good" : "good"); break;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.UseDarkTitleBar(Handle);
    }
}
