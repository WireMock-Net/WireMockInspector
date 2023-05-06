# How to use it

1. Install [WireMockInspector](https://github.com/WireMock-Net/WireMockInspector) dotnet tool

	```shell
	dotnet tool install WireMockInspector --global --no-cache --ignore-failed-sources
	```

2. Install `WireMock.Net.Extensions.WireMockInspector` nuget packet to your test project
3. Example usage in the code

	```cs
	using var wireMock = WireMockServer.Start(new WireMockServerSettings()
						{
							StartAdminInterface = true,
							Port = 9095
						});

	// Call Inspect() run WireMockInspect and attach it to current WireMockServer instance
	wireMock.Inspect();
	```