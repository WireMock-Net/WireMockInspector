# WireMockInspector

WireMockInspect is a cross platform UI app that facilitates [WireMock](https://wiremock.org/) troubleshooting.

![](logo.jpg)

## How to install

WireMockInspector is distributed as `dotnet tool` so it can be easily install on `Windows/MacOS/Linux` with the following command

```
dotnet tool install WireMockInspector --global --no-cache --ignore-failed-sources
```

After installation you can easily run the app by executing `wiremockinspector` command.

## Features
- Presents a list of requests received by `WireMock` server.
- Combines request data with associated mapping.
- Presents a list of all available mappings with the definition
- Generate C# code for defining selected mappings
- WireMockServer settings editor

![](wiremock_basic_features.gif)


## Using WireMockInspector from test code

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
