using Winspace.Models;

namespace Winspace.Services;

public enum WorkspaceState { Inactive, Activating, Active, Deactivating }

public class WorkspaceOrchestrator
{
    private readonly ConfigService _config;
    private readonly ProcessService _process;
    private readonly WindowLayoutService _layout;

    public WorkspaceState State { get; private set; } = WorkspaceState.Inactive;

    public event Action<WorkspaceState>? StateChanged;
    public event Action<string>? StatusMessage;
    public event Action<string>? ErrorOccurred;

    public WorkspaceOrchestrator(ConfigService config, ProcessService process, WindowLayoutService layout)
    {
        _config = config;
        _process = process;
        _layout = layout;
    }

    public async Task ActivateAsync()
    {
        if (State == WorkspaceState.Active || State == WorkspaceState.Activating) return;

        var profile = _config.GetActiveProfile();
        if (profile == null)
        {
            StatusMessage?.Invoke("No active profile found.");
            return;
        }

        SetState(WorkspaceState.Activating);
        var progress = new Progress<string>(msg => StatusMessage?.Invoke(msg));

        try
        {
            await _process.KillAppsAsync(profile, progress);
            await _process.LaunchAppsAsync(profile, progress);
            await _layout.ApplyLayoutAsync(profile.Layout, profile, progress);

            StatusMessage?.Invoke("Workspace active.");
            SetState(WorkspaceState.Active);
        }
        catch (Exception ex)
        {
            var msg = $"Activation error: {ex.GetType().Name} — {ex.Message}";
            StatusMessage?.Invoke(msg);
            ErrorOccurred?.Invoke(msg);
            SetState(WorkspaceState.Inactive);
        }
    }

    public async Task DeactivateAsync()
    {
        if (State == WorkspaceState.Inactive || State == WorkspaceState.Deactivating) return;

        SetState(WorkspaceState.Deactivating);
        StatusMessage?.Invoke("Deactivating workspace…");

        var profile = _config.GetActiveProfile();
        if (profile != null)
        {
            var progress = new Progress<string>(msg => StatusMessage?.Invoke(msg));
            await _process.KillAppsAsync(profile, progress);
        }

        StatusMessage?.Invoke("Workspace inactive.");
        SetState(WorkspaceState.Inactive);
    }

    public async Task ToggleAsync()
    {
        if (State == WorkspaceState.Active)
            await DeactivateAsync();
        else
            await ActivateAsync();
    }

    private void SetState(WorkspaceState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
