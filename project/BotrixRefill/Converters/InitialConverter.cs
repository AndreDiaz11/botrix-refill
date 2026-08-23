using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BotrixRefill.Converters;

public class InitialConverter : IValueConverter
{
    public static readonly InitialConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value as string;
        return string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
