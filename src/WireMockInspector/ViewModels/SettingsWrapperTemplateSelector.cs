using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace WireMockInspector.ViewModels;

public class SettingsWrapperTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control? Build(object? param)
    {

        if (param is SettingsWrapper w)
        {
            var templateName = w.Type switch
            {
                { IsEnum: true } => "Enum",
                { Name: "Nullable`1" } n when n.GenericTypeArguments[0].IsEnum => "Enum",
                var x => x.ToString() switch
                {
                    "System.String" => "string",
                    "System.Boolean" => "bool",
                    "System.Nullable`1[System.Boolean]" => "bool?",
                    "System.Int32" => "int",
                    "System.Nullable`1[System.Int32]" => "int?",
                    "System.String[]" => "stringlist",
                    _ => "object"
                }
            };

            return AvailableTemplates[templateName].Build(param);
        }

        throw new ArgumentNullException(nameof(param));
    }

    public bool Match(object? data)
    {
        if (data is SettingsWrapper w)
        {
            return true;
        }

        return false;
    }
}