using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace VanDao.Desktop;

/// <summary>
/// Dải game kiểu "TBH: Task Bar Hero": một cửa sổ ngang, không viền, nền trong suốt,
/// nằm sát ngay trên thanh taskbar và luôn hiển thị khi làm việc khác.
///
/// - Chuột phải (hoặc nút ⋯): menu nhanh — bố cục màn hình, kích thước, ghim, nền trong suốt, độ trong.
/// - Kéo ⋮⋮ để đặt tự do; chọn lại một bố cục để neo về taskbar.
/// - Nút "Tu luyện": mở Đột phá / Túi đồ / Luyện đan trong cửa sổ bật lên phía trên dải.
/// - Shift+F12: đặt lại vị trí khi bị lạc. Shift+F11: đặt lại kích thước.
/// - Biểu tượng ở khay hệ thống: ẩn/hiện và thoát.
/// </summary>
public sealed class GameWindow : Form
{
    private static readonly Size BaseSize = new(280, 200);
    private const int HotkeyResetPos = 1, HotkeyResetScale = 2;

    private readonly string _url;
    private readonly WindowSettings _s = WindowSettings.Load();
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly ContextMenuStrip _menu = new() { Renderer = Theme.Renderer, ShowImageMargin = true };
    private readonly NotifyIcon _tray = new() { Text = "Vấn Đạo", Icon = SystemIcons.Application, Visible = true };
    private readonly System.Windows.Forms.Timer _dockTimer = new() { Interval = 2000 };
    private PanelWindow? _panel;
    private bool _placing;

    public GameWindow(string serverUrl)
    {
        _url = serverUrl.TrimEnd('/') + "/";

        Text = "Vấn Đạo";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        Controls.Add(_web);

        BuildMenu();
        _tray.ContextMenuStrip = _menu;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleVisible(); };

        ApplySize();
        ApplyTransparency();
        ApplyPin();
        Opacity = 1.0; // độ trong cố định 100%
        PlaceStrip();

        // người dùng kéo xong → chuyển sang vị trí tự do
        ResizeEnd += (_, _) =>
        {
            if (_placing) return;
            _s.Dock = StripDock.Manual;
            (_s.X, _s.Y) = (Left, Top);
            _s.Screen = Screen.FromControl(this).DeviceName;
            _s.Save();
            if (_panel?.Visible == true) _panel.PlaceNearStrip();
        };

        // taskbar đổi vị trí, đổi độ phân giải, cắm thêm màn hình → neo lại
        _dockTimer.Tick += (_, _) => { if (_s.Dock != StripDock.Manual) PlaceStrip(); };
        _dockTimer.Start();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;

        Load += async (_, _) => await InitWebAsync();
        FormClosed += (_, _) =>
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            _tray.Visible = false;
            _tray.Dispose();
        };
    }

    public Task SyncAsync() => WebHost.SyncAsync(_web);

    /// <summary>Đột phá ở cửa sổ Tu luyện → phát hiệu ứng trên dải game.</summary>
    public void PlayBreak(string kind)
    {
        if (_web.CoreWebView2 is null || kind is not ("good" or "gold" or "bad")) return;
        _ = _web.ExecuteScriptAsync($"window.playBreak && window.playBreak('{kind}', true)");
    }

    // ================= Kích thước, vị trí =================

    private void ApplySize()
    {
        var size = LogicalToDeviceUnits(new Size((int)(BaseSize.Width * _s.Scale), (int)(BaseSize.Height * _s.Scale)));
        _placing = true;
        Size = size;
        _placing = false;
        PlaceStrip();
    }

    private Screen TargetScreen() =>
        Screen.AllScreens.FirstOrDefault(sc => sc.DeviceName == _s.Screen) ?? Screen.PrimaryScreen!;

    /// <summary>Tính vị trí theo bố cục đã chọn, sát mép thanh taskbar.</summary>
    private void PlaceStrip()
    {
        var sc = TargetScreen();
        var area = sc.WorkingArea;
        var dock = _s.Dock;

        if (dock == StripDock.Manual && _s.X is int mx && _s.Y is int my)
        {
            var r = new Rectangle(mx, my, Width, Height);
            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(r))) { MoveTo(new Point(mx, my)); return; }
            dock = StripDock.Auto; // vị trí cũ nằm ngoài màn hình
        }

        if (dock is StripDock.Auto or StripDock.Manual)
        {
            // taskbar ở trên thì neo dưới nó, còn lại neo phía trên taskbar
            var taskbarOnTop = area.Top > sc.Bounds.Top;
            dock = taskbarOnTop ? StripDock.TopRight : StripDock.BottomRight;
        }

        var m = LogicalToDeviceUnits(8);
        var top = dock is StripDock.TopLeft or StripDock.TopCenter or StripDock.TopRight;
        var y = top ? area.Top : area.Bottom - Height;
        var x = dock switch
        {
            StripDock.BottomLeft or StripDock.TopLeft => area.Left + m,
            StripDock.BottomCenter or StripDock.TopCenter => area.Left + (area.Width - Width) / 2,
            _ => area.Right - Width - m,
        };
        MoveTo(new Point(x, y));
    }

    private void MoveTo(Point p)
    {
        if (Location == p) return;
        _placing = true;
        Location = p;
        _placing = false;
        if (_panel?.Visible == true) _panel.PlaceNearStrip();
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => BeginInvoke(PlaceStrip);

    private void SetDock(StripDock dock)
    {
        _s.Dock = dock;
        _s.Save();
        PlaceStrip();
    }

    private void ResetPosition()
    {
        _s.Dock = StripDock.Auto;
        _s.X = _s.Y = null;
        _s.Screen = null;
        _s.Save();
        ShowStrip();
        PlaceStrip();
    }

    // ================= Ghim, trong suốt =================

    private void ApplyPin()
    {
        TopMost = _s.Pinned;
        if (_panel is not null) _panel.TopMost = _s.Pinned;
    }

    /// <summary>
    /// Nền trong suốt bằng cách cắt hình cửa sổ (window region) theo khung game bo góc.
    /// Không dùng TransparencyKey vì WebView2 vẽ ở lớp riêng nên Windows coi cả cửa sổ là "trong suốt"
    /// và mọi cú click đều xuyên qua.
    /// </summary>
    private void ApplyTransparency()
    {
        BackColor = Theme.Ink;
        _web.DefaultBackgroundColor = Theme.Ink;
        if (!_s.Transparent) SetRegion(null);
        if (_web.CoreWebView2 is null) return;
        _ = _web.ExecuteScriptAsync($"window.setOpaque && window.setOpaque({(_s.Transparent ? "false" : "true")})");
        if (_s.Transparent) _ = _web.ExecuteScriptAsync("window.reportShape && window.reportShape(true)");
    }

    private void ApplyShape(JsonElement m)
    {
        if (!_s.Transparent) return;
        try
        {
            var k = m.GetProperty("card");
            using var card = RoundRect(k.GetProperty("x").GetInt32(), k.GetProperty("y").GetInt32(),
                                       k.GetProperty("w").GetInt32(), k.GetProperty("h").GetInt32(), k.GetProperty("r").GetInt32());
            var region = new Region(card);
            if (m.TryGetProperty("circle", out var c))
            {
                using var circle = new GraphicsPath();
                circle.AddEllipse(c.GetProperty("x").GetInt32(), c.GetProperty("y").GetInt32(),
                                  c.GetProperty("size").GetInt32(), c.GetProperty("size").GetInt32());
                region.Union(circle);
            }
            SetRegion(region);
        }
        catch (Exception) { /* dữ liệu lạ thì giữ nguyên hình cũ */ }
    }

    private void SetRegion(Region? region)
    {
        var old = Region;
        Region = region;
        old?.Dispose();
    }

    private static GraphicsPath RoundRect(int x, int y, int w, int h, int r)
    {
        var p = new GraphicsPath();
        var d = Math.Max(1, r * 2);
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // ================= Menu nhanh (chuột phải / khay hệ thống) =================

    private void BuildMenu()
    {
        _menu.Opening += (_, _) => RefreshMenu();
    }

    private void RefreshMenu()
    {
        _menu.Items.Clear();

        var layout = new ToolStripMenuItem("Bố cục màn hình");
        foreach (var (dock, name) in new[]
        {
            (StripDock.Auto, "Tự động theo thanh taskbar"),
            (StripDock.BottomLeft, "Trên taskbar · trái"),
            (StripDock.BottomCenter, "Trên taskbar · giữa"),
            (StripDock.BottomRight, "Trên taskbar · phải"),
            (StripDock.TopLeft, "Mép trên · trái"),
            (StripDock.TopCenter, "Mép trên · giữa"),
            (StripDock.TopRight, "Mép trên · phải"),
        })
        {
            var d = dock;
            layout.DropDownItems.Add(new ToolStripMenuItem(name, null, (_, _) => SetDock(d)) { Checked = _s.Dock == d });
        }
        layout.DropDownItems.Add(new ToolStripSeparator());
        layout.DropDownItems.Add(new ToolStripMenuItem("Tự do (kéo ⋮⋮ để di chuyển)") { Checked = _s.Dock == StripDock.Manual, Enabled = false });
        _menu.Items.Add(layout);

        var size = new ToolStripMenuItem("Kích thước");
        foreach (var k in new[] { 0.75, 1.0, 1.25, 1.5, 2.0 })
        {
            var v = k;
            size.DropDownItems.Add(new ToolStripMenuItem($"{k * 100:0}%", null, (_, _) => { _s.Scale = v; _s.Save(); ApplySize(); })
                { Checked = Math.Abs(_s.Scale - k) < 0.001 });
        }
        _menu.Items.Add(size);

        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripMenuItem("Ghim luôn hiển thị", null, (_, _) => { _s.Pinned = !_s.Pinned; _s.Save(); ApplyPin(); })
            { Checked = _s.Pinned });
        _menu.Items.Add(new ToolStripMenuItem("Nền trong suốt", null, (_, _) => { _s.Transparent = !_s.Transparent; _s.Save(); ApplyTransparency(); })
            { Checked = _s.Transparent });
        _menu.Items.Add(new ToolStripMenuItem("Đặt lại vị trí", null, (_, _) => ResetPosition()) { ShortcutKeyDisplayString = "Shift+F12" });
        _menu.Items.Add(new ToolStripMenuItem("Đặt lại kích thước", null, (_, _) => { _s.Scale = 1; _s.Save(); ApplySize(); }) { ShortcutKeyDisplayString = "Shift+F11" });
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(Visible ? "Ẩn xuống khay hệ thống" : "Hiện dải game", null, (_, _) => ToggleVisible());
        _menu.Items.Add("Thoát", null, (_, _) => Close());

        foreach (ToolStripItem item in _menu.Items) item.ForeColor = Theme.Paper;
    }

    private void ShowMenuAtCursor() => _menu.Show(Cursor.Position);

    private void ToggleVisible()
    {
        if (Visible) { _panel?.Hide(); Hide(); }
        else ShowStrip();
    }

    private void ShowStrip()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OpenPanel(string tab)
    {
        _panel ??= new PanelWindow(this, _s, _url);
        _panel.ShowTab(tab);
    }

    // ================= WebView2 =================

    private async Task InitWebAsync()
    {
        try
        {
            await WebHost.InitAsync(_web, OnMessage);
            _web.CoreWebView2.ContextMenuRequested += (_, e) =>
            {
                e.Handled = true; // thay menu trình duyệt bằng menu của game
                ShowMenuAtCursor();
            };
            _web.Source = new Uri($"{_url}?mode=strip{(_s.Transparent ? "" : "&opaque=1")}");
        }
        catch (WebView2RuntimeNotFoundException)
        {
            Hide();
            _tray.ShowBalloonTip(8000, "Vấn Đạo",
                "Máy chưa cài Microsoft Edge WebView2 Runtime nên game mở trên trình duyệt. Thoát game từ biểu tượng ở khay hệ thống.",
                ToolTipIcon.Info);
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
    }

    private void OnMessage(string type, JsonElement msg)
    {
        switch (type)
        {
            case "drag": Native.BeginDrag(this); break;
            case "menu": ShowMenuAtCursor(); break;
            case "open-panel": OpenPanel(msg.TryGetProperty("tab", out var t) ? t.GetString() ?? "req" : "req"); break;
            case "changed": _ = _panel?.SyncAsync(); break;
            case "shape": ApplyShape(msg); break;
        }
    }

    // ================= Phím tắt toàn cục =================

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.RegisterHotKey(Handle, HotkeyResetPos, Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_F12);
        Native.RegisterHotKey(Handle, HotkeyResetScale, Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_F11);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Native.UnregisterHotKey(Handle, HotkeyResetPos);
        Native.UnregisterHotKey(Handle, HotkeyResetScale);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            if (m.WParam == HotkeyResetPos) ResetPosition();
            else if (m.WParam == HotkeyResetScale) { _s.Scale = 1; _s.Save(); ApplySize(); ShowStrip(); }
        }
        base.WndProc(ref m);
    }

    // không lấy focus khi bấm vào lần đầu → bớt giành focus với cửa sổ đang làm việc
    protected override bool ShowWithoutActivation => true;
}
