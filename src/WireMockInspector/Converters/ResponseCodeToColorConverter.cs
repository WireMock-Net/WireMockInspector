using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WireMockInspector.Converters;

public class ResponseCodeToColorConverter : IValueConverter
{
    public static readonly ResponseCodeToColorConverter Instance = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string responseCode && int.TryParse(responseCode, out int statusCode))
        {
            var color = (statusCode / 100) switch
            {
                1 => Color.FromRgb(173, 216, 230),
                2 => Color.FromRgb(0, 150, 0),
                3 => Color.FromRgb(255, 165, 0),
                4 => Color.FromRgb(255, 0, 0),
                5 => Color.FromRgb(139, 0, 0),
                _ => Colors.Gray
            };

            return new SolidColorBrush(color);
        }

        // Return a default color if the input is not a valid string or cannot be parsed to an integer
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}