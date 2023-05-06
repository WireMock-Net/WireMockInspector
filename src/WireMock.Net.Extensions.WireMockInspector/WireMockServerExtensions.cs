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
    public static void Inspect(this IWireMockServer wireMock, [CallerMemberName] string callerMethodName = "")
    {
        if (wireMock.IsStarted == false)
            throw new InvalidOperationException("WireMock server is not started");

        var inspectorProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "wiremockinspector",
            Arguments = $"attach --adminUrl {wireMock.Url} --autoLoad --instanceName {callerMethodName}",
            UseShellExecute = false
        });

        inspectorProcess?.WaitForExit();
    }
}