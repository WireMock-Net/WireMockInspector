using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WireMockInspector.CodeGenerators;

internal static class CSharpFormatter
{
    #region Reserved Keywords

    private static readonly HashSet<string> CSharpReservedKeywords = new(new[]
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    });

    #endregion

    private const string Null = "null";


    public static string? TryToConvertJsonToAnonymousObject(object input, int ind = 0)
    {
        try
        {
            return input switch
            {
                JToken token => ConvertJsonToAnonymousObjectDefinition(token, ind),
                string text => ConvertJsonToAnonymousObjectDefinition(JToken.Parse(text), ind),
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    public static string ConvertJsonToAnonymousObjectDefinition(JToken token, int ind = 0)
    {
        return token switch
        {
            JArray jArray => FormatArray(jArray, ind),
            JObject jObject => FormatObject(jObject, ind),
            JValue jValue => jValue.Type switch
            {
                JTokenType.None => Null,
                JTokenType.Integer => jValue.Value != null
                    ? string.Format(CultureInfo.InvariantCulture, "{0}", jValue.Value)
                    : Null,
                JTokenType.Float => jValue.Value != null
                    ? string.Format(CultureInfo.InvariantCulture, "{0}", jValue.Value)
                    : Null,
                JTokenType.String => ToCSharpStringLiteral(jValue.Value?.ToString()),
                JTokenType.Boolean => jValue.Value != null
                    ? string.Format(CultureInfo.InvariantCulture, "{0}", jValue.Value).ToLower()
                    : Null,
                JTokenType.Null => Null,
                JTokenType.Undefined => Null,
                JTokenType.Date when jValue.Value is DateTime dateValue =>
                    $"DateTime.Parse({ToCSharpStringLiteral(dateValue.ToString("s"))})",
                _ => $"UNHANDLED_CASE: {jValue.Type}"
            },
            _ => $"UNHANDLED_CASE: {token}"
        };
    }

    public static string ToCSharpStringLiteral(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (value.Contains('\n'))
        {
            var escapedValue = value?.Replace("\"", "\"\"") ?? string.Empty;
            return $"@\"{escapedValue}\"";
        }
        else
        {
            var escapedValue = value?.Replace("\"", "\\\"") ?? string.Empty;
            return $"\"{escapedValue}\"";
        }
    }

    public static string FormatPropertyName(string propertyName)
    {
        return CSharpReservedKeywords.Contains(propertyName) ? "@" + propertyName : propertyName;
    }

    private static string FormatObject(JObject jObject, int ind)
    {
        
        var indStr = new string(' ', 4 * ind);
        var indStrSub = new string(' ', 4 * (ind + 1));
        var shouldBeDictionary = jObject.Properties().Any(x => Char.IsDigit(x.Name[0]));

        if (shouldBeDictionary)
        {
            var items = jObject.Properties().Select(x =>  $"[\"{x.Name}\"] = {ConvertJsonToAnonymousObjectDefinition(x.Value, ind + 1)}");
            return $"new Dictionary<string, object>\r\n{indStr}{{\r\n{indStrSub}{string.Join($",\r\n{indStrSub}", items)}\r\n{indStr}}}";   
        }
        else
        {
            var items = jObject.Properties().Select(x =>  $"{FormatPropertyName(x.Name)} = {ConvertJsonToAnonymousObjectDefinition(x.Value, ind + 1)}");
            return $"new\r\n{indStr}{{\r\n{indStrSub}{string.Join($",\r\n{indStrSub}", items)}\r\n{indStr}}}";    
        }
        
        
    }

    private static string FormatArray(JArray jArray, int ind)
    {
        var hasComplexItems = jArray.FirstOrDefault() is JObject or JArray;
        var items = jArray.Select(x => ConvertJsonToAnonymousObjectDefinition(x, hasComplexItems ? ind + 1 : ind));
        if (hasComplexItems)
        {
            var indStr = new string(' ', 4 * ind);
            var indStrSub = new string(' ', 4 * (ind + 1));
            return $"new []\r\n{indStr}{{\r\n{indStrSub}{string.Join($",\r\n{indStrSub}", items)}\r\n{indStr}}}";
        }

        return $"new [] {{ {string.Join(", ", items)} }}";
    }
}