using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winspace.ViewModels;

namespace Winspace.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    public DashboardView(DashboardViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void ToggleTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        (DataContext as DashboardViewModel)?.Toggle();
    }
}
