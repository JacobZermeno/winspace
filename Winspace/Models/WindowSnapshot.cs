namespace Winspace.Models;

public class WindowSnapshot
{
    public string ProcessName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsMaximized { get; set; }

    public WindowSnapshot() { }

    public WindowSnapshot(string processName, int x, int y, int width, int height, bool isMaximized = false)
    {
        ProcessName = processName;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsMaximized = isMaximized;
    }
}
