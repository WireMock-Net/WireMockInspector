using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WireMockInspector.ViewModels;

public class StringMatchConverter : IValueConverter
{
    public static readonly StringMatchConverter Instance = new StringMatchConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parameterString = parameter?.ToString() ?? "null";
        var valueString = value?.ToString() ?? "null";
        return parameterString.Equals(valueString, StringComparison.InvariantCultureIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}