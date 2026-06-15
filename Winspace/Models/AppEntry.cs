using System.Text.Json.Serialization;

namespace Winspace.Models;

public class AppEntry
{
    public string DisplayName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Path);

    public AppEntry() { }

    public AppEntry(string displayName, string path, string arguments = "")
    {
        DisplayName = displayName;
        Path = path;
        Arguments = arguments;
    }
}
