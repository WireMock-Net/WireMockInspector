using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;

namespace WireMockInspector.ViewModels;

public class GithubUpdater
{
    private readonly string _repositorySku;

    public GithubUpdater(string repositorySku)
    {
        _repositorySku = repositorySku;
    }

    public string RepositorySku => _repositorySku;

    public string GetProductFullVersion()
    {
        var assemblyVersion = this.GetType().Assembly.GetName().Version;
        return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }

    public class ReleaseResponse
    {
        public string tag_name { get; set; }
    }
   
    public async Task<(bool isavailable, string latestVersion)> CheckIsNewerVersionAvailable()
    {
        try
        {
            using var httpClient = new HttpClient();
            var currentProductVersionRaw = GetProductFullVersion();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GithubUpdater", currentProductVersionRaw));
            var response = await httpClient.GetAsync($"https://api.github.com/repos/{RepositorySku}/releases/latest").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReleaseResponse>(payload);
                if (string.IsNullOrWhiteSpace(result?.tag_name) == false)
                {
                    var latestVersion = Version.Parse(result.tag_name);

                    var currentVersion = Version.Parse(currentProductVersionRaw);
                    return (latestVersion > currentVersion, result.tag_name);
                }
            }
        }
        catch (Exception e)
        {
            return (false, string.Empty);
        }

        return (false, string.Empty);
    }
}

public class NewVersionInfoViewModel:ViewModelBase
{
    private readonly string _repositorySku;
    
    public string Version { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private bool _isVisible;
    
    public NewVersionInfoViewModel(string repositorySku)
    {
        _repositorySku = repositorySku;
    }

    public void OpenReleaseLog()
    {
        OpenWebsite(@$"https://github.com/{_repositorySku}/releases/");
    }

    private static void OpenWebsite(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url.Replace("&", "^&"),
            UseShellExecute = true,
            CreateNoWindow = false
        });
    }

    public void DismissNewVersion()
    {
        IsVisible = false;
    }
}