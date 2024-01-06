using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WireMock.Server;

namespace WireMock.Net.Extensions.WireMockInspector;

public static class WireMockServerExtensions
{
    /// <summary>
    ///     Run WireMockInspector dotnet tool and attach it to the current WireMockServer instance
    /// </summary>
    /// <remarks>
    ///     The current thread will be blocked until WireMockInspector process exits.
    /// </remarks>
    public static void Inspect(this IWireMockServer wireMock, [CallerMemberName] string title = "", string requestFilters = "")
    {
        if (wireMock.IsStarted == false)
            throw new InvalidOperationException("WireMock server is not started");

        if (wireMock.IsStartedWithAdminInterface == false)
            throw new InvalidOperationException("WireMock service is not started with Admin interface");

        Inspect(wireMock.Url!, title, requestFilters);
    }

    /// <summary>
    ///     Run WireMockInspector dotnet tool and attach it to existing WireMockServer listening under given url
    /// </summary>
    public static void Inspect(string wireMockUrl, [CallerMemberName] string title = "", string requestFilters = "")
    {
        //request.header.traceparent:*0fd18063ed68b122d3e659ecbb4e6f0d*
        try
        {
            var arguments = $"attach --adminUrl {wireMockUrl} --autoLoad --instanceName \"{title}\"";
            if (string.IsNullOrWhiteSpace(requestFilters) == false)
            {
                arguments += $" --requestFilters \"{requestFilters}\"";
            }
            
            var inspectorProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "wiremockinspector",
                Arguments = arguments,
                UseShellExecute = false
            });

            inspectorProcess?.WaitForExit();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException
            (
                message: @"Cannot find installation of WireMockInspector.
Execute the following command to install WireMockInspector dotnet tool:
> dotnet tool install WireMockInspector --global --no-cache --ignore-failed-sources
To get more info please visit https://github.com/WireMock-Net/WireMockInspector",
                innerException: e
            );
        }
    }
}