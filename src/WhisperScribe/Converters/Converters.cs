using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WhisperScribe.Models;

namespace WhisperScribe.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool v && v;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        return value switch
        {
            TranscriptionStatus.Completed => app.Resources["Brush.Success"],
            TranscriptionStatus.Failed => app.Resources["Brush.Danger"],
            TranscriptionStatus.Processing => app.Resources["Brush.Highlight"],
            _ => app.Resources["Brush.TextMuted"]
        };
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !(value is bool b && b);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => !(value is bool b && b);
}

public class SecondsToDurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double seconds ? TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss") : "—";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
