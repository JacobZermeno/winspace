using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Winspace.Models;
using Winspace.Services;
using Winspace.ViewModels;
using Winspace.Views;

namespace Winspace;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    public static bool IsMinimizeToTrayEnabled { get; private set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        // ── Single-instance guard ────────────────────────────────────────────
        _mutex = new Mutex(true, "Winspace_SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Winspace is already running.", "Winspace",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        // ── Global exception handlers ────────────────────────────────────────
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Winspace — Unhandled Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(args.ExceptionObject?.ToString(), "Winspace — Fatal Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // ── Bootstrap services ───────────────────────────────────────────────
        var config     = new ConfigService();
        config.Load();

        var process    = new ProcessService(config);
        var layout     = new WindowLayoutService();
        var orchestrator = new WorkspaceOrchestrator(config, process, layout);

        // ── Apply saved theme ────────────────────────────────────────────────
        IsMinimizeToTrayEnabled = config.Settings.MinimizeToTrayOnClose;
        MainViewModel.ApplyTheme(config.Settings.Accent, config.Settings.DarkMode, config.Settings.ViewMode);

        // ── Build ViewModels ─────────────────────────────────────────────────
        var dashVm   = new DashboardViewModel(orchestrator, config);
        var configVm = new ConfigViewModel(config, process, layout);
        var mainVm   = new MainViewModel(dashVm, configVm);

        // ── System tray ──────────────────────────────────────────────────────
        _trayIcon = BuildTrayIcon(orchestrator, mainVm);

        // ── Main window ──────────────────────────────────────────────────────
        _mainWindow = new MainWindow(mainVm);

        if (!config.Settings.StartMinimized)
            _mainWindow.Show();

        orchestrator.StateChanged += state =>
        {
            IsMinimizeToTrayEnabled = config.Settings.MinimizeToTrayOnClose;
            UpdateTrayIcon(_trayIcon, state);
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private TaskbarIcon BuildTrayIcon(WorkspaceOrchestrator orchestrator, MainViewModel mainVm)
    {
        var tray = new TaskbarIcon
        {
            ToolTipText = "Winspace — Inactive",
            IconSource  = TryLoadIcon()
        };

        var ctxMenu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open Winspace" };
        openItem.Click += (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };

        var toggleItem = new MenuItem { Header = "Activate Workspace" };
        toggleItem.Click += async (_, _) =>
        {
            await orchestrator.ToggleAsync();
            toggleItem.Header = orchestrator.State == WorkspaceState.Active
                ? "Deactivate Workspace" : "Activate Workspace";
        };

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            IsMinimizeToTrayEnabled = false;
            Current.Shutdown();
        };

        ctxMenu.Items.Add(openItem);
        ctxMenu.Items.Add(new Separator());
        ctxMenu.Items.Add(toggleItem);
        ctxMenu.Items.Add(new Separator());
        ctxMenu.Items.Add(exitItem);

        tray.ContextMenu = ctxMenu;

        tray.TrayMouseDoubleClick += (_, _) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };

        return tray;
    }

    private static void UpdateTrayIcon(TaskbarIcon tray, WorkspaceState state)
    {
        tray.ToolTipText = state switch
        {
            WorkspaceState.Active      => "Winspace — Active",
            WorkspaceState.Activating  => "Winspace — Activating…",
            WorkspaceState.Deactivating=> "Winspace — Deactivating…",
            _                          => "Winspace — Inactive"
        };
    }

    private static System.Windows.Media.ImageSource? TryLoadIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/Icons/winspace.ico");
            return new System.Windows.Media.Imaging.BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }
}
