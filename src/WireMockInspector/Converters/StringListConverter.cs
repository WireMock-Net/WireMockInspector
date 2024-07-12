using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WireMockInspector.Converters;

public class StringListConverter : IValueConverter
{
    public static readonly StringListConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string[] sl)
        {
            return string.Join(",", sl);
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s.Split(",");
        }

        return null;
    }
}
