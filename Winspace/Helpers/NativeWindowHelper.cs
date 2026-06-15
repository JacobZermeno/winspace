using System.Runtime.InteropServices;
using System.Text;

namespace Winspace.Helpers;

public static class NativeWindowHelper
{
    // ── Constants ────────────────────────────────────────────────────────────
    public const uint WM_CLOSE = 0x0010;
    public const uint SW_RESTORE = 9;
    public const uint SW_MAXIMIZE = 3;
    public const uint SW_SHOW = 5;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const int GWL_STYLE = -16;
    public const long WS_MAXIMIZE = 0x01000000L;

    // ── Delegates ────────────────────────────────────────────────────────────
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // ── Structs ──────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public uint length;
        public uint flags;
        public uint showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    // ── P/Invoke Declarations ────────────────────────────────────────────────
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern long GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    // ── Helpers ──────────────────────────────────────────────────────────────

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var len = GetWindowTextLength(hWnd);
        if (len == 0) return string.Empty;
        var sb = new StringBuilder(len + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static bool IsWindowMaximized(IntPtr hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_STYLE);
        return (style & WS_MAXIMIZE) != 0;
    }

    /// <summary>
    /// Enumerates all visible top-level windows, returning a map of PID → hWnd.
    /// For processes with multiple windows, the first visible one wins.
    /// </summary>
    public static Dictionary<uint, IntPtr> GetVisibleWindowsByPid()
    {
        var result = new Dictionary<uint, IntPtr>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            if (GetWindowTextLength(hWnd) == 0) return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (!result.ContainsKey(pid))
                result[pid] = hWnd;
            return true;
        }, IntPtr.Zero);
        return result;
    }

    /// <summary>
    /// Moves and resizes a window. Restores it first if maximized.
    /// </summary>
    public static void MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool maximize = false)
    {
        if (maximize)
        {
            ShowWindow(hWnd, SW_MAXIMIZE);
            return;
        }

        // Restore first so SetWindowPos takes effect
        ShowWindow(hWnd, SW_RESTORE);
        SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }
}
