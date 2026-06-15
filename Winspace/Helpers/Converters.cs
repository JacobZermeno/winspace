using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Winspace.Helpers;

/// <summary>
/// Returns AccentBrush when IsActive=true, SubtleBrush when busy, TileBrush otherwise.
/// Binds: IsActive (bool), IsBusy (bool)
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public class ActiveStateToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return GetResource("TileBrush");
        bool isActive = values[0] is true;
        bool isBusy = values[1] is true;

        if (isBusy) return GetResource("SubtleBrush");
        return isActive ? GetResource("AccentBrush") : GetResource("TileBrush");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Brush GetResource(string key)
        => Application.Current.Resources[key] as Brush
           ?? Brushes.Gray;
}

/// <summary>
/// Inverts a boolean (for IsEnabled = !IsBusy).
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>
/// Converts bool to Visibility. Pass "Invert" as parameter to reverse.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (parameter is string s && s == "Invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts null to Visibility (Collapsed when null).
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the string value equals the ConverterParameter (case-insensitive).
/// ConvertBack sets the string to the parameter when IsChecked becomes true.
/// Used for RadioButton.IsChecked ↔ string enum property.
/// </summary>
[ValueConversion(typeof(string), typeof(bool))]
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? (parameter?.ToString() ?? string.Empty) : Binding.DoNothing;
}
