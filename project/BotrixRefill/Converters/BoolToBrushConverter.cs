using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BotrixRefill.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ok = value is true;
        return ok ? Brushes.ForestGreen : Brushes.Crimson;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
