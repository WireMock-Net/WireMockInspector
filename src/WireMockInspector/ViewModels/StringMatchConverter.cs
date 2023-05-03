using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

namespace WireMockInspector.ViewModels;

public class StringMatchConverter : IValueConverter
{
    public static readonly StringMatchConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parameterString = parameter?.ToString() ?? "null";
        var valueString = value?.ToString() ?? "null";
        return parameterString.Equals(valueString, StringComparison.InvariantCultureIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumItemsConverter: IValueConverter
{
    public static readonly EnumItemsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type t)
        {
            if (t.Name == "Nullable`1" && t.GenericTypeArguments[0].IsEnum)
            {
                t = t.GenericTypeArguments[0];
            }

            if (t.IsEnum)
            {
                return t.GetEnumValues();
            }
        }
        return Array.Empty<string>();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class StringListConverter: IValueConverter
{
    public static readonly StringListConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string[] sl)
        {
            return string.Join(",", sl);
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s.Split(",");
        }

        return null;
    }
}


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
                {IsEnum: true} => "Enum",
                {Name: "Nullable`1" } n when n.GenericTypeArguments[0].IsEnum => "Enum",
                var x => x.ToString() switch
                {
                    "System.String"  => "string",
                    "System.Boolean"  => "bool",
                    "System.Nullable`1[System.Boolean]"  => "bool?",
                    "System.Int32"  => "int",
                    "System.Nullable`1[System.Int32]"  => "int?",
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