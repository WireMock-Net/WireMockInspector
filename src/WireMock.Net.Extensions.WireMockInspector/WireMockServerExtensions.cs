﻿using System;
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

        if (wireMock.IsStartedWithAdminInterface == false)
            throw new InvalidOperationException("WireMock service is not started with Admin interface");

        try
        {
            var inspectorProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "wiremockinspector",
                Arguments = $"attach --adminUrl {wireMock.Url} --autoLoad --instanceName {callerMethodName}",
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