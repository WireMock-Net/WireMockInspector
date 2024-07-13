using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WireMockInspector.Converters;

public class RequestMethodToColorConverter : IValueConverter
{
    public static readonly RequestMethodToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string requestMethod)
        {
            // Define your color scheme based on request methods
            var color = requestMethod.ToUpper() switch
            {
                "GET" => Color.FromRgb(0, 150, 0),          // Green
                "POST" => Color.FromRgb(0, 0, 255),         // Blue
                "PUT" => Color.FromRgb(255, 165, 0),        // Orange
                "DELETE" => Color.FromRgb(255, 0, 0),       // Red
                "PATCH" => Color.FromRgb(128, 0, 128),      // Purple
                "HEAD" => Color.FromRgb(0, 255, 255),       // Cyan
                "OPTIONS" => Color.FromRgb(255, 255, 0),    // Yellow
                "UNLINK" => Color.FromRgb(255, 20, 147),    // Dark Pink
                "LINK" => Color.FromRgb(0, 206, 209),       // Dark Turquoise
                "COPY" => Color.FromRgb(85, 107, 47),       // Dark Olive Green
                "PURGE" => Color.FromRgb(47, 79, 79),       // Dark Slate Gray
                "LOCK" => Color.FromRgb(153, 50, 204),      // Dark Orchid
                "UNLOCK" => Color.FromRgb(255, 140, 0),     // Dark Orange
                "PROPFIND" => Color.FromRgb(184, 134, 11),  // Dark Goldenrod
                "VIEW" => Color.FromRgb(143, 188, 143),     // Dark Sea Green
                _ => Colors.Gray
            };

            return new SolidColorBrush(color);
        }

        // Return a default color if the input is not a valid string
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}