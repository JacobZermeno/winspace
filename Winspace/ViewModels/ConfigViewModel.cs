using System.Collections.ObjectModel;
using Winspace.Helpers;
using Winspace.Models;
using Winspace.Services;

namespace Winspace.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly ProcessService _process;
    private readonly WindowLayoutService _layoutService;

    // ── Profile ──────────────────────────────────────────────────────────────
    public ObservableCollection<string> ProfileNames { get; } = new();
    private string _selectedProfileName = string.Empty;
    public string SelectedProfileName
    {
        get => _selectedProfileName;
        set
        {
            if (SetProperty(ref _selectedProfileName, value))
                LoadProfile(value);
        }
    }

    // ── Launch List ──────────────────────────────────────────────────────────
    public ObservableCollection<AppEntry> LaunchList { get; } = new();
    private string _newLaunchName = string.Empty;
    private string _newLaunchPath = string.Empty;
    private string _newLaunchArgs = string.Empty;
    private AppEntry? _selectedLaunchEntry;

    public string NewLaunchName { get => _newLaunchName; set => SetProperty(ref _newLaunchName, value); }
    public string NewLaunchPath { get => _newLaunchPath; set => SetProperty(ref _newLaunchPath, value); }
    public string NewLaunchArgs { get => _newLaunchArgs; set => SetProperty(ref _newLaunchArgs, value); }
    public AppEntry? SelectedLaunchEntry { get => _selectedLaunchEntry; set => SetProperty(ref _selectedLaunchEntry, value); }

    // ── Kill List ────────────────────────────────────────────────────────────
    public ObservableCollection<string> KillList { get; } = new();
    public ObservableCollection<string> RunningProcesses { get; } = new();
    private string? _selectedKillEntry;
    private string? _selectedRunningProcess;

    public string? SelectedKillEntry { get => _selectedKillEntry; set => SetProperty(ref _selectedKillEntry, value); }
    public string? SelectedRunningProcess { get => _selectedRunningProcess; set => SetProperty(ref _selectedRunningProcess, value); }

    // ── Whitelist ────────────────────────────────────────────────────────────
    public ObservableCollection<string> UserWhitelist { get; } = new();
    public IReadOnlyCollection<string> SystemWhitelist { get; } = ProcessService.GetHardcodedWhitelist().ToList();
    private string _newWhitelistEntry = string.Empty;
    private string? _selectedWhitelistEntry;

    public string NewWhitelistEntry { get => _newWhitelistEntry; set => SetProperty(ref _newWhitelistEntry, value); }
    public string? SelectedWhitelistEntry { get => _selectedWhitelistEntry; set => SetProperty(ref _selectedWhitelistEntry, value); }

    // ── Layout ───────────────────────────────────────────────────────────────
    public ObservableCollection<string> LayoutPresetNames { get; } = new();
    private string _selectedPresetName = string.Empty;
    private string _layoutStatusMessage = string.Empty;

    public string SelectedPresetName
    {
        get => _selectedPresetName;
        set
        {
            if (SetProperty(ref _selectedPresetName, value))
                SavePresetSelection();
        }
    }
    public string LayoutStatusMessage { get => _layoutStatusMessage; set => SetProperty(ref _layoutStatusMessage, value); }

    // ── Theme Settings ───────────────────────────────────────────────────────
    public ObservableCollection<string> AccentColorNames { get; } = new(Enum.GetNames<AccentColor>());
    public ObservableCollection<string> ViewModeNames { get; } = new(Enum.GetNames<ViewMode>());

    private string _selectedAccent = string.Empty;
    private bool _darkMode;
    private string _selectedViewMode = string.Empty;

    public string SelectedAccent
    {
        get => _selectedAccent;
        set
        {
            if (SetProperty(ref _selectedAccent, value)) ApplyThemeSettings();
        }
    }
    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            if (SetProperty(ref _darkMode, value)) ApplyThemeSettings();
        }
    }
    public string SelectedViewMode
    {
        get => _selectedViewMode;
        set
        {
            if (SetProperty(ref _selectedViewMode, value)) ApplyThemeSettings();
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public RelayCommand AddLaunchEntryCommand { get; }
    public RelayCommand RemoveLaunchEntryCommand { get; }
    public RelayCommand BrowseLaunchPathCommand { get; }
    public RelayCommand AddToKillListCommand { get; }
    public RelayCommand RemoveFromKillListCommand { get; }
    public RelayCommand RefreshRunningProcessesCommand { get; }
    public RelayCommand AddWhitelistEntryCommand { get; }
    public RelayCommand RemoveWhitelistEntryCommand { get; }
    public AsyncRelayCommand CaptureLayoutCommand { get; }
    public RelayCommand AddProfileCommand { get; }
    public RelayCommand RemoveProfileCommand { get; }

    public event Action<AccentColor, bool, ViewMode>? ThemeChanged;

    public ConfigViewModel(ConfigService config, ProcessService process, WindowLayoutService layoutService)
    {
        _config = config;
        _process = process;
        _layoutService = layoutService;

        AddLaunchEntryCommand = new RelayCommand(AddLaunchEntry);
        RemoveLaunchEntryCommand = new RelayCommand(RemoveLaunchEntry, () => SelectedLaunchEntry != null);
        BrowseLaunchPathCommand = new RelayCommand(BrowseLaunchPath);
        AddToKillListCommand = new RelayCommand(AddToKillList, () => SelectedRunningProcess != null);
        RemoveFromKillListCommand = new RelayCommand(RemoveFromKillList, () => SelectedKillEntry != null);
        RefreshRunningProcessesCommand = new RelayCommand(RefreshRunningProcesses);
        AddWhitelistEntryCommand = new RelayCommand(AddWhitelistEntry, () => !string.IsNullOrWhiteSpace(NewWhitelistEntry));
        RemoveWhitelistEntryCommand = new RelayCommand(RemoveWhitelistEntry, () => SelectedWhitelistEntry != null);
        CaptureLayoutCommand = new AsyncRelayCommand(CaptureLayoutAsync);
        AddProfileCommand = new RelayCommand(AddProfile);
        RemoveProfileCommand = new RelayCommand(RemoveProfile, () => ProfileNames.Count > 1);

        // Populate preset names
        foreach (var kv in WindowLayout.PresetDisplayNames)
            LayoutPresetNames.Add(kv.Value);

        Initialize();
    }

    public void Initialize()
    {
        ProfileNames.Clear();
        foreach (var p in _config.Profiles)
            ProfileNames.Add(p.Name);

        _selectedProfileName = _config.Settings.ActiveProfileName;
        OnPropertyChanged(nameof(SelectedProfileName));
        LoadProfile(_selectedProfileName);

        UserWhitelist.Clear();
        foreach (var w in _config.Settings.UserWhitelist)
            UserWhitelist.Add(w);

        _selectedAccent = _config.Settings.Accent.ToString();
        _darkMode = _config.Settings.DarkMode;
        _selectedViewMode = _config.Settings.ViewMode.ToString();
        OnPropertyChanged(nameof(SelectedAccent));
        OnPropertyChanged(nameof(DarkMode));
        OnPropertyChanged(nameof(SelectedViewMode));

        RefreshRunningProcesses();
    }

    private void LoadProfile(string profileName)
    {
        var profile = _config.Profiles.FirstOrDefault(p => p.Name == profileName);
        if (profile == null) return;

        _config.SetActiveProfile(profileName);

        LaunchList.Clear();
        foreach (var e in profile.LaunchList) LaunchList.Add(e);

        KillList.Clear();
        foreach (var k in profile.KillList) KillList.Add(k);

        var presetName = WindowLayout.PresetDisplayNames[profile.Layout.Preset];
        _selectedPresetName = presetName;
        OnPropertyChanged(nameof(SelectedPresetName));
    }

    private void AddLaunchEntry()
    {
        if (string.IsNullOrWhiteSpace(NewLaunchPath)) return;
        var entry = new AppEntry(
            string.IsNullOrWhiteSpace(NewLaunchName) ? System.IO.Path.GetFileNameWithoutExtension(NewLaunchPath) : NewLaunchName,
            NewLaunchPath, NewLaunchArgs);
        LaunchList.Add(entry);
        SaveCurrentProfile();
        NewLaunchName = string.Empty;
        NewLaunchPath = string.Empty;
        NewLaunchArgs = string.Empty;
    }

    private void RemoveLaunchEntry()
    {
        if (SelectedLaunchEntry == null) return;
        LaunchList.Remove(SelectedLaunchEntry);
        SaveCurrentProfile();
    }

    private void BrowseLaunchPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            NewLaunchPath = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NewLaunchName))
                NewLaunchName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void AddToKillList()
    {
        if (SelectedRunningProcess == null) return;
        if (!KillList.Contains(SelectedRunningProcess))
        {
            KillList.Add(SelectedRunningProcess);
            SaveCurrentProfile();
        }
    }

    private void RemoveFromKillList()
    {
        if (SelectedKillEntry == null) return;
        KillList.Remove(SelectedKillEntry);
        SaveCurrentProfile();
    }

    private void RefreshRunningProcesses()
    {
        RunningProcesses.Clear();
        foreach (var name in _process.GetKillableProcessNames())
            RunningProcesses.Add(name);
    }

    private void AddWhitelistEntry()
    {
        if (string.IsNullOrWhiteSpace(NewWhitelistEntry)) return;
        if (!UserWhitelist.Contains(NewWhitelistEntry, StringComparer.OrdinalIgnoreCase))
        {
            UserWhitelist.Add(NewWhitelistEntry);
            _config.Settings.UserWhitelist.Add(NewWhitelistEntry);
            _config.Save();
        }
        NewWhitelistEntry = string.Empty;
    }

    private void RemoveWhitelistEntry()
    {
        if (SelectedWhitelistEntry == null) return;
        UserWhitelist.Remove(SelectedWhitelistEntry);
        _config.Settings.UserWhitelist.Remove(SelectedWhitelistEntry);
        _config.Save();
    }

    private async Task CaptureLayoutAsync()
    {
        var profile = _config.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null) return;

        LayoutStatusMessage = "Capturing window positions…";
        await Task.Run(() =>
        {
            var layout = _layoutService.CaptureCurrentLayout(profile);
            profile.Layout = layout;
        });
        _config.Save();
        LayoutStatusMessage = $"Captured {profile.Layout.Snapshots.Count} window(s).";

        // Sync preset selector to Custom
        _selectedPresetName = WindowLayout.PresetDisplayNames[LayoutPreset.Custom];
        OnPropertyChanged(nameof(SelectedPresetName));
    }

    private void SavePresetSelection()
    {
        var profile = _config.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null) return;
        var preset = WindowLayout.PresetDisplayNames
            .FirstOrDefault(kv => kv.Value == SelectedPresetName).Key;
        profile.Layout.Preset = preset;
        _config.Save();
    }

    private void ApplyThemeSettings()
    {
        if (Enum.TryParse<AccentColor>(SelectedAccent, out var accent))
            _config.Settings.Accent = accent;
        if (Enum.TryParse<ViewMode>(SelectedViewMode, out var vm))
            _config.Settings.ViewMode = vm;
        _config.Settings.DarkMode = DarkMode;
        _config.Save();
        ThemeChanged?.Invoke(_config.Settings.Accent, _config.Settings.DarkMode, _config.Settings.ViewMode);
    }

    private void SaveCurrentProfile()
    {
        var profile = _config.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null) return;
        profile.LaunchList = LaunchList.ToList();
        profile.KillList = KillList.ToList();
        _config.Save();
    }

    private void AddProfile()
    {
        var name = $"Profile {ProfileNames.Count + 1}";
        _config.AddProfile(name);
        ProfileNames.Add(name);
        SelectedProfileName = name;
    }

    private void RemoveProfile()
    {
        if (ProfileNames.Count <= 1) return;
        var toRemove = SelectedProfileName;
        _config.RemoveProfile(toRemove);
        ProfileNames.Remove(toRemove);
        SelectedProfileName = ProfileNames.FirstOrDefault() ?? string.Empty;
    }
}
