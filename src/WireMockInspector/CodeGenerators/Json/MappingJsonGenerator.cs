using System.IO;
using System.Text.RegularExpressions;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Admin.Requests;
using WireMockInspector.ViewModels;

namespace WireMockInspector.CodeGenerators.Json;

public class MappingJsonGenerator : CodeGenerator
{
    public static string EscapeString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return JsonConvert.SerializeObject(value);
    }

    public static string Generate(LogRequestModel logRequest, LogResponseModel logResponse, MappingCodeGeneratorConfigViewModel config)
    {
        var options = new TemplateOptions();
        options.ValueConverters.Add(o => o is JToken t ? t.ToString() : null);
        options.Filters.AddFilter("escape_json", (input, arguments, templateContext) => new StringValue(EscapeString(input.ToStringValue())));
        var parser = new FluidParser();

        var templateCode = "";
        if (config.SelectedTemplate == DefaultTemplateName)
        {
            templateCode = ReadEmbeddedResource("WireMockInspector.CodeGenerators.Json.default_template.liquid");
        }
        else if (string.IsNullOrWhiteSpace(config.SelectedTemplate) == false)
        {
            var templatePath = Path.Combine(PathHelper.GetTemplateDir(), config.SelectedTemplate);
            if (File.Exists(templatePath))
            {
                templateCode = File.ReadAllText(templatePath);
            }
        }

        if (parser.TryParse(templateCode, out var ftemplate, out var error))
        {
            var reader = new JsonDataSourceReader();
            var data = reader.Read(JsonConvert.SerializeObject(
            new
            {
                request = new
                {
                    logRequest.ClientIP,
                    logRequest.DateTime,
                    logRequest.Path,
                    FullPath = GetFullPath(logRequest),
                    logRequest.AbsolutePath,
                    logRequest.Url,
                    logRequest.AbsoluteUrl,
                    logRequest.ProxyUrl,
                    Query = logRequest.Query.OrNullWhenEmpty(),
                    logRequest.Method,
                    Headers = logRequest.Headers.OrNullWhenEmpty(),
                    Cookies = logRequest.Cookies.OrNullWhenEmpty(),
                    logRequest.Body,
                    BodyAsJson = (TryParseJson(logRequest.Body) ?? logRequest.BodyAsJson)?.ToString(),
                    logRequest.BodyAsBytes,
                    logRequest.BodyEncoding,
                    logRequest.DetectedBodyType,
                    logRequest.DetectedBodyTypeFromContentType
                },
                response = new
                {
                    logResponse.StatusCode,
                    Headers = logResponse.Headers.OrNullWhenEmpty(),
                    logResponse.BodyDestination,
                    logResponse.Body,
                    BodyAsJson = (TryParseJson(logResponse.Body) ?? logResponse.BodyAsJson)?.ToString(),
                    logResponse.BodyAsBytes,
                    logResponse.BodyAsFile,
                    logResponse.BodyAsFileIsCached,
                    logResponse.BodyOriginal,
                    logResponse.BodyEncoding,
                    logResponse.DetectedBodyType,
                    logResponse.DetectedBodyTypeFromContentType,
                    logResponse.FaultType,
                    logResponse.FaultPercentage
                },
                config
            }));
            var result = ftemplate.Render(new TemplateContext(new
            {
                data
            }, options));
            
            return Regex.Replace(result, @",(?=\s*[\]}])", "");
        }

        return error;
    }
}