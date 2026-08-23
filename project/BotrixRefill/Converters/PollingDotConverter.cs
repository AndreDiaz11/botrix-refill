using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BotrixRefill.Converters;

public class PollingDotConverter : IValueConverter
{
    public static readonly PollingDotConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var on = value is true;
        return new SolidColorBrush(on ? Color.Parse("#12B76A") : Color.Parse("#9CA3AF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
