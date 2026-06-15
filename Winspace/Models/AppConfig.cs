namespace Winspace.Models;

public class AppConfig
{
    public AppSettings Settings { get; set; } = new();
    public List<WorkspaceProfile> Profiles { get; set; } = new();

    public WorkspaceProfile? GetActiveProfile()
        => Profiles.FirstOrDefault(p => p.Name == Settings.ActiveProfileName)
           ?? Profiles.FirstOrDefault();
}
