namespace Winspace.Models;

public enum LayoutPreset
{
    Custom,
    SplitFiftyFifty,
    ThreeColumnGrid,
    PrimaryCenterFocus,
    FullScreen
}

public class WindowLayout
{
    public string Name { get; set; } = "Custom Layout";
    public LayoutPreset Preset { get; set; } = LayoutPreset.Custom;
    public List<WindowSnapshot> Snapshots { get; set; } = new();

    public static readonly Dictionary<LayoutPreset, string> PresetDisplayNames = new()
    {
        { LayoutPreset.Custom,             "Custom (Captured)" },
        { LayoutPreset.SplitFiftyFifty,    "50/50 Split Screen" },
        { LayoutPreset.ThreeColumnGrid,    "Three-Column Grid" },
        { LayoutPreset.PrimaryCenterFocus, "Primary Center Focus" },
        { LayoutPreset.FullScreen,         "Full Screen" },
    };
}
