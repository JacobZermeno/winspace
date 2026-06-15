using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Winspace.Helpers;
using Winspace.Services;

namespace Winspace.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly WorkspaceOrchestrator _orchestrator;
    private readonly ConfigService _config;
    private readonly Dispatcher _dispatcher;

    private string _statusMessage = "Ready.";
    private bool _isActive;
    private bool _isBusy;
    private bool _hasError;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            SetProperty(ref _isActive, value);
            OnPropertyChanged(nameof(ToggleLabel));
            OnPropertyChanged(nameof(ToggleSubLabel));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                _dispatcher.InvokeAsync(CommandManager.InvalidateRequerySuggested);
        }
    }

    public string ActiveProfileName => _config.Settings.ActiveProfileName;
    public string ToggleLabel => IsActive ? "ACTIVE" : "INACTIVE";
    public string ToggleSubLabel => IsActive ? "Click to deactivate workspace" : "Click to activate workspace";

    public RelayCommand ToggleCommand { get; }

    public DashboardViewModel(WorkspaceOrchestrator orchestrator, ConfigService config)
    {
        _orchestrator = orchestrator;
        _config = config;
        _dispatcher = Application.Current.Dispatcher;

        // Use a synchronous RelayCommand that fires-and-forgets the async work.
        // CanExecute is driven by IsBusy (which is always set on the UI thread via Dispatch),
        // so CommandManager.InvalidateRequerySuggested() reliably re-enables the button.
        ToggleCommand = new RelayCommand(
            () => _ = ToggleAsync(),
            () => !IsBusy
        );

        _orchestrator.StateChanged += state => Dispatch(() =>
        {
            IsActive = state == WorkspaceState.Active;
            IsBusy = state == WorkspaceState.Activating || state == WorkspaceState.Deactivating;
            if (state == WorkspaceState.Active || state == WorkspaceState.Inactive)
                HasError = false;
        });

        _orchestrator.StatusMessage += msg => Dispatch(() =>
        {
            StatusMessage = msg;
        });

        _orchestrator.ErrorOccurred += msg => Dispatch(() =>
        {
            StatusMessage = msg;
            HasError = true;
        });
    }

    public void Toggle()
    {
        if (IsBusy) return;
        _ = ToggleAsync();
    }

    private async Task ToggleAsync()
    {
        HasError = false;
        IsBusy = true;
        try
        {
            await _orchestrator.ToggleAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.GetType().Name} — {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Dispatch(Action action)
    {
        if (_dispatcher.CheckAccess())
            action();
        else
            _dispatcher.Invoke(action);
    }

    public void RefreshProfile() => OnPropertyChanged(nameof(ActiveProfileName));
}
