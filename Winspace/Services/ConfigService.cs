using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Winspace.Models;

namespace Winspace.Services;

public class ConfigService
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Winspace");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private AppConfig _config = new();

    public AppConfig Config => _config;
    public AppSettings Settings => _config.Settings;
    public List<WorkspaceProfile> Profiles => _config.Profiles;

    public void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            _config = CreateDefault();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            _config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefault();
        }
        catch
        {
            _config = CreateDefault();
            Save();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(_config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public WorkspaceProfile? GetActiveProfile()
        => _config.GetActiveProfile();

    public WorkspaceProfile AddProfile(string name)
    {
        var profile = new WorkspaceProfile(name);
        _config.Profiles.Add(profile);
        Save();
        return profile;
    }

    public void RemoveProfile(string name)
    {
        _config.Profiles.RemoveAll(p => p.Name == name);
        if (_config.Settings.ActiveProfileName == name)
            _config.Settings.ActiveProfileName = _config.Profiles.FirstOrDefault()?.Name ?? "Default";
        Save();
    }

    public void SetActiveProfile(string name)
    {
        _config.Settings.ActiveProfileName = name;
        Save();
    }

    private static AppConfig CreateDefault()
    {
        var profile = new WorkspaceProfile("Work")
        {
            LaunchList = new List<AppEntry>
            {
                new("Notepad", "notepad.exe"),
            },
            KillList = new List<string> { "Steam", "Discord" },
            Layout = new WindowLayout
            {
                Preset = LayoutPreset.SplitFiftyFifty
            }
        };

        return new AppConfig
        {
            Settings = new AppSettings
            {
                ActiveProfileName = "Work",
                UserWhitelist = new List<string>()
            },
            Profiles = new List<WorkspaceProfile> { profile }
        };
    }
}
