using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Admin.Requests;
using WireMockInspector.ViewModels;

namespace WireMockInspector.CodeGenerators;

public static class MappingCodeGenerator
{

    class JsonDataSourceReader 
    {
        object? ConvertJsonToObject(JToken xDocument)
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

    public static string EscapeStringForCSharp(string value) => CSharpFormatter.ToCSharpStringLiteral(value);

    private static string ReadEmbeddedResource(string resourceName)
    {
        // Get the current assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Using stream to read the embedded file.
        using var stream = assembly.GetManifestResourceStream(resourceName);
        // Make sure the resource is available
        if (stream == null) throw new FileNotFoundException("The specified embedded resource cannot be found.", resourceName);
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public const string DefaultTemplateForCSharp = "C# (default)";
    public const string DefaultTemplateForJSON = "JSON (default)";


    private static JToken? TryParseJson(string? payload)
    {
        try
        {
            return payload switch
            {
                {  } => JToken.Parse(payload),
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }
    
    private static string EscapeStringForJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return JsonConvert.SerializeObject(value);
    }
    
    public static MarkdownCode GenerateCode(LogRequestModel logRequest, LogResponseModel logResponse, MappingCodeGeneratorConfigViewModel config)
    { 
        var options = new TemplateOptions();
        options.ValueConverters.Add(o => o is JToken t?  t.ToString(): null );
        options.Filters.AddFilter("escape_json", (input, arguments, templateContext) => new StringValue(EscapeStringForJson(input.ToStringValue())));
        options.Filters.AddFilter("escape_string_for_csharp", (input, arguments, templateContext) => new StringValue(EscapeStringForCSharp(input.ToStringValue()) ));
        options.Filters.AddFilter("format_as_anonymous_object", (input, arguments, templateContext) =>
        {
            var ind = arguments.Values.FirstOrDefault() is NumberValue nv ? (int)nv.ToNumberValue() : 0;
            
            return input switch
            {
                StringValue dv => CSharpFormatter.TryToConvertJsonToAnonymousObject(dv.ToStringValue(), ind) switch
                {
                    { } s => new StringValue(s),
                    _ => NilValue.Instance, 
                },
                _ => input
            };
        });

        var parser = new FluidParser();
        var (lang, templateCode) = GetTemplateCode(config);
        if (parser.TryParse(templateCode, out var ftemplate, out var error))
        {
            var reader = new JsonDataSourceReader();

            var data = reader.Read(JsonConvert.SerializeObject(
            new
            {
                request = new
                {
                    ClientIP = logRequest.ClientIP,
                    DateTime = logRequest.DateTime,
                    Path = logRequest.Path,
                    FullPath = GetFullPath(logRequest),
                    AbsolutePath = logRequest.AbsolutePath,
                    Url = logRequest.Url,
                    AbsoluteUrl = logRequest.AbsoluteUrl,
                    ProxyUrl = logRequest.ProxyUrl,
                    Query = logRequest.Query.OrNullWhenEmpty(),
                    Method = logRequest.Method,
                    Headers = logRequest.Headers.OrNullWhenEmpty(),
                    Cookies = logRequest.Cookies.OrNullWhenEmpty(),
                    Body = logRequest.Body,
                    BodyAsJson = (TryParseJson(logRequest.Body) ?? logRequest.BodyAsJson)?.ToString(),
                    BodyAsBytes = logRequest.BodyAsBytes,
                    BodyEncoding = logRequest.BodyEncoding,
                    DetectedBodyType = logRequest.DetectedBodyType,
                    DetectedBodyTypeFromContentType = logRequest.DetectedBodyTypeFromContentType
                },
                response = new
                {
                    StatusCode = logResponse.StatusCode,
                    Headers = logResponse.Headers.OrNullWhenEmpty(),
                    BodyDestination = logResponse.BodyDestination,
                    Body = logResponse.Body,
                    BodyAsJson = (TryParseJson(logResponse.Body) ?? logResponse.BodyAsJson)?.ToString(),
                    BodyAsBytes = logResponse.BodyAsBytes,
                    BodyAsFile = logResponse.BodyAsFile,
                    BodyAsFileIsCached = logResponse.BodyAsFileIsCached,
                    BodyOriginal = logResponse.BodyOriginal,
                    BodyEncoding = logResponse.BodyEncoding,
                    DetectedBodyType = logResponse.DetectedBodyType,
                    DetectedBodyTypeFromContentType = logResponse.DetectedBodyTypeFromContentType,
                    FaultType = logResponse.FaultType,
                    FaultPercentage = logResponse.FaultPercentage
                },
                config
            }));
            var result = ftemplate.Render(new TemplateContext(new
            {
               data = data 
            }, options));
            return new MarkdownCode(lang, result);
        }

        return new MarkdownCode("text", $"Error: {error}");
        
    }

    private static (string lang, string template) GetTemplateCode(MappingCodeGeneratorConfigViewModel config)
    {
        if (config.SelectedTemplate == DefaultTemplateForCSharp)
        {
            return ("cs", ReadEmbeddedResource("WireMockInspector.CodeGenerators.default_template.liquid"));
        }
        
        if (config.SelectedTemplate == DefaultTemplateForJSON)
        {
            return ("json", ReadEmbeddedResource("WireMockInspector.CodeGenerators.json_definition_template.liquid"));
        }
        
        if(string.IsNullOrWhiteSpace(config.SelectedTemplate) == false)
        {
            var templatePath = Path.Combine(PathHelper.GetTemplateDir(), config.SelectedTemplate);
            if (File.Exists(templatePath))
            {
                return ("cs", File.ReadAllText(templatePath));
            }
        }

        return ("cs",string.Empty);
    }

    private static string GetFullPath(LogRequestModel logRequest)
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