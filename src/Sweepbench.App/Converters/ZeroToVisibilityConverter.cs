using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sweepbench.App.Converters;

/// <summary>Visible when the bound count is zero — drives the "no items yet" empty state.</summary>
public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
