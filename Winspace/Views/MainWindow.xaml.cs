using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Winspace.ViewModels;

namespace Winspace.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel vm) : this()
    {
        _vm = vm;
        DataContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentView))
                AnimateViewTransition(vm.CurrentView);
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        if (App.IsMinimizeToTrayEnabled)
            Hide();
        else
            Application.Current.Shutdown();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (App.IsMinimizeToTrayEnabled)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void AnimateViewTransition(MainView targetView)
    {
        const double duration = 0.15;
        const double offset = 20;

        var dashSlide = DashView.RenderTransform as System.Windows.Media.TranslateTransform
                        ?? new System.Windows.Media.TranslateTransform();
        var cfgSlide  = CfgView.RenderTransform as System.Windows.Media.TranslateTransform
                        ?? new System.Windows.Media.TranslateTransform();

        if (targetView == MainView.Dashboard)
        {
            // Config slides out right, dash slides in from left
            AnimateX(cfgSlide, 0, offset, duration);
            AnimateOpacity(CfgView, 1, 0, duration);

            dashSlide.X = -offset;
            DashView.Opacity = 0;
            AnimateX(dashSlide, -offset, 0, duration);
            AnimateOpacity(DashView, 0, 1, duration);
        }
        else
        {
            // Dash slides out left, config slides in from right
            AnimateX(dashSlide, 0, -offset, duration);
            AnimateOpacity(DashView, 1, 0, duration);

            cfgSlide.X = offset;
            CfgView.Opacity = 0;
            AnimateX(cfgSlide, offset, 0, duration);
            AnimateOpacity(CfgView, 0, 1, duration);
        }
    }

    private static void AnimateX(System.Windows.Media.TranslateTransform t, double from, double to, double secs)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(secs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        t.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
    }

    private static void AnimateOpacity(UIElement el, double from, double to, double secs)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(secs));
        el.BeginAnimation(OpacityProperty, anim);
    }
}
