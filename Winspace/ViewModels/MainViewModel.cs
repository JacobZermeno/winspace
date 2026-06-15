using Winspace.Helpers;
using Winspace.Models;
using Winspace.Services;

namespace Winspace.ViewModels;

public enum MainView { Dashboard, Config }

public class MainViewModel : ViewModelBase
{
    private MainView _currentView = MainView.Dashboard;

    public MainView CurrentView
    {
        get => _currentView;
        set
        {
            SetProperty(ref _currentView, value);
            OnPropertyChanged(nameof(IsDashboardView));
            OnPropertyChanged(nameof(IsConfigView));
        }
    }

    public bool IsDashboardView => CurrentView == MainView.Dashboard;
    public bool IsConfigView => CurrentView == MainView.Config;

    public DashboardViewModel Dashboard { get; }
    public ConfigViewModel Config { get; }

    public RelayCommand NavigateDashboardCommand { get; }
    public RelayCommand NavigateConfigCommand { get; }

    public MainViewModel(DashboardViewModel dashboard, ConfigViewModel config)
    {
        Dashboard = dashboard;
        Config = config;

        NavigateDashboardCommand = new RelayCommand(() => CurrentView = MainView.Dashboard);
        NavigateConfigCommand = new RelayCommand(() =>
        {
            Config.Initialize();
            CurrentView = MainView.Config;
        });

        Config.ThemeChanged += (accent, dark, viewMode) =>
            App.Current?.Dispatcher.Invoke(() => ApplyTheme(accent, dark, viewMode));
    }

    public static void ApplyTheme(AccentColor accent, bool darkMode, ViewMode viewMode)
    {
        var accentHex = accent switch
        {
            AccentColor.Cobalt  => "#0078D7",
            AccentColor.Crimson => "#C42B1C",
            AccentColor.Teal    => "#00B4D8",
            AccentColor.Emerald => "#107C10",
            _                   => "#0078D7"
        };

        var res = System.Windows.Application.Current.Resources;
        res["AccentBrush"]       = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentHex));
        res["AccentHoverBrush"]  = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(accentHex));

        if (darkMode)
        {
            res["CanvasBrush"]      = BrushFromHex("#1A1A1A");
            res["TileBrush"]        = BrushFromHex("#2D2D2D");
            res["NavBrush"]         = BrushFromHex("#111111");
            res["ForegroundBrush"]  = BrushFromHex("#FFFFFF");
            res["SubtleBrush"]      = BrushFromHex("#888888");
            res["BorderBrush"]      = BrushFromHex("#3A3A3A");
            res["InputBrush"]       = BrushFromHex("#252525");
        }
        else
        {
            res["CanvasBrush"]      = BrushFromHex("#F2F2F2");
            res["TileBrush"]        = BrushFromHex("#FFFFFF");
            res["NavBrush"]         = BrushFromHex("#E0E0E0");
            res["ForegroundBrush"]  = BrushFromHex("#111111");
            res["SubtleBrush"]      = BrushFromHex("#666666");
            res["BorderBrush"]      = BrushFromHex("#CCCCCC");
            res["InputBrush"]       = BrushFromHex("#FAFAFA");
        }
    }

    private static System.Windows.Media.SolidColorBrush BrushFromHex(string hex)
        => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
}
