using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace WireMockInspector.ViewModels;

public class IconSelectorConverter : IValueConverter
{
    public static readonly IconSelectorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isWatching)
        {
            // When watching, show pause icon; otherwise show play icon
            var resourceKey = isWatching ? "pause_circle_regular" : "play_circle_regular";
            
            // Try to get the geometry from resources
            if (Avalonia.Application.Current?.TryGetResource(resourceKey, null, out var resource) == true && resource is StreamGeometry geometry)
            {
                return geometry;
            }
        }

        // Fallback: return play icon key
        if (Avalonia.Application.Current?.TryGetResource("play_circle_regular", null, out var fallback) == true && fallback is StreamGeometry geo)
        {
            return geo;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
