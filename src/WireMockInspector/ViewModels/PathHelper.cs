using System;
using System.IO;

namespace WireMockInspector.ViewModels;

public static class PathHelper
{
    public static string GetTemplateDir()
    {
        var settingsPath = GetSettingsDir();
        var templateDir = Path.Combine(settingsPath, "templates");
        if (Directory.Exists(templateDir) == false)
        {
            Directory.CreateDirectory(templateDir);
        }

        return templateDir;
    }


    public static string GetSettingsDir()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WireMockInspector");
        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}