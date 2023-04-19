using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WireMockInspector.ViewModels;

public class AppSettingsManager<T>
{
    private readonly T _defaultSettings;
    private readonly string _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly()!.GetName().Name!, $"{nameof(T)}.json");

    public AppSettingsManager(T defaultSettings)
    {
        _defaultSettings = defaultSettings;
    }

    public async Task<T> LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var payload = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(payload) ?? _defaultSettings;
            }
            catch
            {
                return _defaultSettings;
            }
        }
        return _defaultSettings;
    }

    public async Task UpdateSettings(Action<T> updateFunc)
    {
        var currentSettings = await LoadSettings().ConfigureAwait(false);
        updateFunc(currentSettings);
        await PersistSettings(currentSettings).ConfigureAwait(false);
        
    }

    private async Task PersistSettings(T currentSettings)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(currentSettings);
            var directoryName = Path.GetDirectoryName(_settingsFilePath)!;
            if (Directory.Exists(directoryName) == false)
            {
                Directory.CreateDirectory(directoryName);
            }
            await File.WriteAllTextAsync(_settingsFilePath, payload).ConfigureAwait(false);
        }
        catch
        {

        }
        
    }
}

