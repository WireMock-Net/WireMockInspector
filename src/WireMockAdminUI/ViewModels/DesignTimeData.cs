using System;

namespace WireMockAdminUI.ViewModels;

public static class DesignTimeData
{
    public static MainWindowViewModel MainViewModel { get; set; } = new MainWindowViewModel()
    {
        Requests =
        {
            new RequestViewModel()
            {
                Timestamp = DateTime.Now,
                IsMatched = true,
                Path = "/api/v1.0/weather?lat=10.99&lon=44.34",
                Method = "POST",
                StatusCode = 200
            },
            new RequestViewModel()
            {
                Timestamp = DateTime.Now,
                IsMatched = false,
                Path = "/api/v1.0/weather?lat=10.99&lon=44.34",
                Method = "GET",
                StatusCode = 404
            },
        }
    };
}