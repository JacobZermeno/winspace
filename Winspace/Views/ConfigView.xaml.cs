using System.Windows.Controls;
using Winspace.ViewModels;

namespace Winspace.Views;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();
    }

    public ConfigView(ConfigViewModel vm) : this()
    {
        DataContext = vm;
    }
}
