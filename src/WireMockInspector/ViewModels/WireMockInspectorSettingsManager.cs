namespace WireMockInspector.ViewModels;

public class WireMockInspectorSettings
{
    public string AdminUrl { get; set; }
}

public class WireMockInspectorSettingsManager : AppSettingsManager<WireMockInspectorSettings>
{
    public WireMockInspectorSettingsManager() : base(new WireMockInspectorSettings()
    {
        AdminUrl = "http://localhost:9091"
    })
    {
    }
}