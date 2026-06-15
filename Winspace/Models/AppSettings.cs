namespace Winspace.Models;

public enum AccentColor
{
    Cobalt,
    Crimson,
    Teal,
    Emerald
}

public enum ViewMode
{
    List,
    Tile
}

public class AppSettings
{
    public AccentColor Accent { get; set; } = AccentColor.Cobalt;
    public bool DarkMode { get; set; } = true;
    public ViewMode ViewMode { get; set; } = ViewMode.Tile;
    public string ActiveProfileName { get; set; } = "Default";
    public List<string> UserWhitelist { get; set; } = new();
    public int GracefulCloseTimeoutMs { get; set; } = 5000;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
}
