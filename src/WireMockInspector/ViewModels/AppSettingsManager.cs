using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WireMockInspector.Templates;

namespace WireMockInspector.ViewModels;

public class AppSettingsManager<T>
{
    private readonly T _defaultSettings;
    private readonly string _settingsDir;
    private readonly string _settingsFilePath;
    private readonly string _settingsTemplateDir;

    public AppSettingsManager(T defaultSettings)
    {
        _defaultSettings = defaultSettings;
        _settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly()!.GetName().Name!);
        _settingsFilePath = Path.Combine(_settingsDir, $"{nameof(T)}.json");
        _settingsTemplateDir = Path.Combine(_settingsDir, "templates");
    }

    public async Task<IReadOnlyList<Template>> LoadTemplates()
    {
        if (Directory.Exists(_settingsDir) && Directory.Exists(_settingsTemplateDir))
        {
            var result = new List<Template>();
            foreach (var x in Directory.EnumerateFiles(_settingsTemplateDir, "*.liquid", SearchOption.TopDirectoryOnly))
            {
                result.Add(new Template()
                {
                    FilePath = x,
                    Content = await File.ReadAllTextAsync(x)
                });
            }

            return result;
        }

        return Array.Empty<Template>();
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

