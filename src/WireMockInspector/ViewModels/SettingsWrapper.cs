using System;
using System.Reflection;
using Avalonia.Data;
using ReactiveUI;

namespace WireMockInspector.ViewModels;

public class SettingsWrapper: ViewModelBase
{
    private readonly object _obj;
    private readonly PropertyInfo _property;

    public SettingsWrapper(object obj, PropertyInfo property, string? namePrefix = null)
    {
        _obj = obj;
        _property = property;
        Name = $"{namePrefix}.{property.Name}".Trim('.');
        TypeDescription = property.PropertyType switch
        {
            { IsEnum: true } => "enumeration",
            { Name: "Nullable`1" } n when n.GenericTypeArguments[0].IsEnum => "enumeration, optional",
            var x => x.ToString() switch
            {
                "System.String" => "string",
                "System.Boolean" => "bool",
                "System.Nullable`1[System.Boolean]" => "bool, optional",
                "System.Int32" => "int",
                "System.Nullable`1[System.Int32]" => "int, optional",
                "System.String[]" => "coma separated list of strings",
                _ => "unknown"
            }
        };
    }

    public string Name { get; set; }


    public Type Type => _property.PropertyType;

    public string TypeDescription { get; set; }

    public object Value
    {
        get => _property.GetValue(_obj);
        set
        {
            if (value == null)
            {
                _property.SetValue(_obj, null);
                this.RaisePropertyChanged();
                return;
            }

            try
            {
                var t = Type;
                if (Type.Name == "Nullable`1")
                {
                    t = Type.GenericTypeArguments[0];
                }

                var res = Convert.ChangeType(value, t);
                _property.SetValue(_obj, res);
                this.RaisePropertyChanged();
            }
            catch
            {
                throw new DataValidationException("Invalid value");
            }
        }
    }
}