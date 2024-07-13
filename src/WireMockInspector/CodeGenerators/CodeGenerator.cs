using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Newtonsoft.Json.Linq;
using WireMock.Admin.Requests;

namespace WireMockInspector.CodeGenerators;

public class CodeGenerator
{
    public class JsonDataSourceReader
    {
        private static object? ConvertJsonToObject(JToken xDocument)
        {
            return xDocument switch
            {
                JArray jArray => jArray.Select(ConvertJsonToObject).ToArray(),
                JObject jObject => jObject.Properties().ToDictionary(x => x.Name, x => ConvertJsonToObject(x.Value)),
                JValue jValue => jValue.Value,
                _ => null
            };
        }

        public object? Read(string content)
        {
            var json = JToken.Parse(content);

            if (json is JObject jo && jo.ContainsKey("$schema"))
            {
                jo.Remove("$schema");
            }

            return ConvertJsonToObject(json);
        }
    }

    public static string ReadEmbeddedResource(string resourceName)
    {
        // Get the current assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Using stream to read the embedded file.
        using var stream = assembly.GetManifestResourceStream(resourceName);
        // Make sure the resource is available
        if (stream == null) throw new FileNotFoundException("The specified embedded resource cannot be found.", resourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
    public const string DefaultTemplateName = "(default)";


    public static JToken? TryParseJson(string? payload)
    {
        try
        {
            return payload switch
            {
                { } => JToken.Parse(payload),
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static string GetFullPath(LogRequestModel logRequest)
    {
        var query = HttpUtility.ParseQueryString("");
        if (logRequest.Query is { } requestQuery)
        {
            foreach (var p in requestQuery)
            {
                query[p.Key] = p.Value.ToString();
            }
        }
        var fullQuery = query.ToString();
        if (string.IsNullOrWhiteSpace(fullQuery) == false)
        {
            return $"{logRequest.Path}?{fullQuery}";
        }
        return logRequest.Path;
    }
}