using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WireMockInspector.ViewModels;

public class WatchStatusConverter : IValueConverter
{
    public static readonly WatchStatusConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WatchConnectionStatus status)
        {
            return status switch
            {
                WatchConnectionStatus.Disconnected => "Disconnected",
                WatchConnectionStatus.Connecting => "Connecting...",
                WatchConnectionStatus.Connected => "Connected",
                WatchConnectionStatus.Reconnecting => "Reconnecting...",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public enum WatchConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
