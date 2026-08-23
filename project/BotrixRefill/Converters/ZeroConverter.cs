using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BotrixRefill.Converters;

public class ZeroConverter : IValueConverter
{
    public static readonly ZeroConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int i && i == 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
