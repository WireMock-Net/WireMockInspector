using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WireMockInspector.Converters;

public class EnumItemsConverter : IValueConverter
{
    public static readonly EnumItemsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type t)
        {
            if (t.Name == "Nullable`1" && t.GenericTypeArguments[0].IsEnum)
            {
                t = t.GenericTypeArguments[0];
            }

            if (t.IsEnum)
            {
                return t.GetEnumValues();
            }
        }
        return Array.Empty<string>();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
