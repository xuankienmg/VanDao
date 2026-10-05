using System.Runtime.InteropServices;

namespace VanDao.Desktop;

internal static class Native
{
    public const int WM_HOTKEY = 0x0312;
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HTCAPTION = 0x2;
    public const uint MOD_SHIFT = 0x4;
    public const uint MOD_NOREPEAT = 0x4000;
    public const uint VK_F11 = 0x7A;
    public const uint VK_F12 = 0x7B;

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    /// <summary>Bắt đầu kéo cửa sổ như đang kéo thanh tiêu đề (dùng cho cửa sổ không viền).</summary>
    public static void BeginDrag(Form form)
    {
        ReleaseCapture();
        SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, IntPtr.Zero);
    }

    /// <summary>Thanh tiêu đề tối trên Windows 10 (20H1+) và Windows 11.</summary>
    public static void UseDarkTitleBar(IntPtr hwnd)
    {
        try { int on = 1; DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); }
        catch { /* Windows cũ thì bỏ qua */ }
    }
}
