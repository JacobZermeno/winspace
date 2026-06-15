namespace Winspace.Models;

public class WorkspaceProfile
{
    public string Name { get; set; } = "Default";
    public List<AppEntry> LaunchList { get; set; } = new();
    public List<string> KillList { get; set; } = new();
    public WindowLayout Layout { get; set; } = new();

    public WorkspaceProfile() { }

    public WorkspaceProfile(string name)
    {
        Name = name;
    }
}
