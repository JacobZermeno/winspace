using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Winspace.Helpers;

/// <summary>
/// Attached property that shows a placeholder text in a TextBox when it is empty and unfocused.
/// Usage: helpers:PlaceholderHelper.Placeholder="Type here…"
/// </summary>
public static class PlaceholderHelper
{
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder",
            typeof(string),
            typeof(PlaceholderHelper),
            new PropertyMetadata(string.Empty, OnPlaceholderChanged));

    public static string GetPlaceholder(DependencyObject obj)
        => (string)obj.GetValue(PlaceholderProperty);

    public static void SetPlaceholder(DependencyObject obj, string value)
        => obj.SetValue(PlaceholderProperty, value);

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;

        tb.Loaded -= TextBox_Loaded;
        tb.Loaded += TextBox_Loaded;
        tb.TextChanged -= TextBox_TextChanged;
        tb.TextChanged += TextBox_TextChanged;
        tb.GotFocus -= TextBox_GotFocus;
        tb.GotFocus += TextBox_GotFocus;
        tb.LostFocus -= TextBox_LostFocus;
        tb.LostFocus += TextBox_LostFocus;
    }

    private static void TextBox_Loaded(object sender, RoutedEventArgs e) => Refresh((TextBox)sender);
    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh((TextBox)sender);
    private static void TextBox_GotFocus(object sender, RoutedEventArgs e) => Refresh((TextBox)sender);
    private static void TextBox_LostFocus(object sender, RoutedEventArgs e) => Refresh((TextBox)sender);

    private static void Refresh(TextBox tb)
    {
        var placeholder = GetPlaceholder(tb);
        if (string.IsNullOrEmpty(placeholder)) return;

        if (string.IsNullOrEmpty(tb.Text) && !tb.IsFocused)
        {
            tb.Foreground = Application.Current.Resources["SubtleBrush"] as Brush
                            ?? SystemColors.GrayTextBrush;
            // Use Tag to temporarily display placeholder
            tb.Text = placeholder;
            tb.Tag = "placeholder_active";
        }
        else if (tb.Tag as string == "placeholder_active" && (tb.IsFocused || tb.Text != placeholder))
        {
            tb.Tag = null;
            tb.Text = string.Empty;
            tb.Foreground = Application.Current.Resources["ForegroundBrush"] as Brush
                            ?? SystemColors.ControlTextBrush;
        }
        else if (tb.Tag as string != "placeholder_active")
        {
            tb.Foreground = Application.Current.Resources["ForegroundBrush"] as Brush
                            ?? SystemColors.ControlTextBrush;
        }
    }
}
