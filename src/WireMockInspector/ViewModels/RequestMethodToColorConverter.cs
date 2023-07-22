using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WireMockInspector.ViewModels;

public class RequestMethodToColorConverter : IValueConverter
{
    public static readonly RequestMethodToColorConverter Instance = new RequestMethodToColorConverter();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string requestMethod)
        {
            // Define your color scheme based on request methods
            var color = requestMethod.ToUpper() switch
            {
                "GET" => Color.FromRgb(0, 150, 0),
                "POST" => Color.FromRgb(0, 0, 255),
                "PUT" => Color.FromRgb(255, 165, 0),
                "DELETE" => Color.FromRgb(255, 0, 0),
                "PATCH" => Color.FromRgb(128, 0, 128),
                "HEAD" => Color.FromRgb(0, 255, 255),
                "OPTIONS" => Color.FromRgb(255, 255, 0),
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