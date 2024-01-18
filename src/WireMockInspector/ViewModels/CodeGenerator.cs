using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using DynamicData.Binding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using WireMock.Admin.Requests;

namespace WireMockInspector.ViewModels;

public class MappingCodeGeneratorViewModel : ViewModelBase
{
    private LogRequestModel _request;
    public LogRequestModel Request
    {
        get => _request;
        set => this.RaiseAndSetIfChanged(ref _request, value);
    }

    private LogResponseModel _response;
    public LogResponseModel Response
    {
        get => _response;
        set => this.RaiseAndSetIfChanged(ref _response, value);
    }

    private MappingCodeGeneratorConfigViewModel _config;

    public MappingCodeGeneratorConfigViewModel Config
    {
        get;
        private set;
    } = new MappingCodeGeneratorConfigViewModel();

    private readonly ObservableAsPropertyHelper<MarkdownCode> _outputCode;
    public MarkdownCode OutputCode => _outputCode.Value;


    public MappingCodeGeneratorViewModel()
    {
   
        Config.WhenAnyPropertyChanged()
            .Where(x=> x is not null)
            .Select(x =>
            {
                var code = CodeGenerator.GenerateCSharpCode(Request, Response, x);
                return new MarkdownCode("cs", code);
            }).ToProperty(this, x => x.OutputCode, out _outputCode);
    }
}
public class MappingCodeGeneratorConfigViewModel : ViewModelBase
{
    // Request attributes
    private bool _includeClientIP = true;
    public bool IncludeClientIP
    {
        get => _includeClientIP;
        set => this.RaiseAndSetIfChanged(ref _includeClientIP, value);
    }

    private bool _includeDateTime = true;
    public bool IncludeDateTime
    {
        get => _includeDateTime;
        set => this.RaiseAndSetIfChanged(ref _includeDateTime, value);
    }

    private bool _includePath = true;
    public bool IncludePath
    {
        get => _includePath;
        set => this.RaiseAndSetIfChanged(ref _includePath, value);
    }

    private bool _includeAbsolutePath = true;
    public bool IncludeAbsolutePath
    {
        get => _includeAbsolutePath;
        set => this.RaiseAndSetIfChanged(ref _includeAbsolutePath, value);
    }

    private bool _includeUrl = true;
    public bool IncludeUrl
    {
        get => _includeUrl;
        set => this.RaiseAndSetIfChanged(ref _includeUrl, value);
    }

    private bool _includeAbsoluteUrl = true;
    public bool IncludeAbsoluteUrl
    {
        get => _includeAbsoluteUrl;
        set => this.RaiseAndSetIfChanged(ref _includeAbsoluteUrl, value);
    }

    private bool _includeProxyUrl = true;
    public bool IncludeProxyUrl
    {
        get => _includeProxyUrl;
        set => this.RaiseAndSetIfChanged(ref _includeProxyUrl, value);
    }

    private bool _includeQuery = true;
    public bool IncludeQuery
    {
        get => _includeQuery;
        set => this.RaiseAndSetIfChanged(ref _includeQuery, value);
    }

    private bool _includeMethod = true;
    public bool IncludeMethod
    {
        get => _includeMethod;
        set => this.RaiseAndSetIfChanged(ref _includeMethod, value);
    }

    private bool _includeHeaders = true;
    public bool IncludeHeaders
    {
        get => _includeHeaders;
        set => this.RaiseAndSetIfChanged(ref _includeHeaders, value);
    }

    private bool _includeCookies = true;
    public bool IncludeCookies
    {
        get => _includeCookies;
        set => this.RaiseAndSetIfChanged(ref _includeCookies, value);
    }

    private bool _includeBody = true;
    public bool IncludeBody
    {
        get => _includeBody;
        set => this.RaiseAndSetIfChanged(ref _includeBody, value);
    }

    // Response attributes
    private bool _includeStatusCode = true;
    public bool IncludeStatusCode
    {
        get => _includeStatusCode;
        set => this.RaiseAndSetIfChanged(ref _includeStatusCode, value);
    }

    private bool _includeHeadersResponse = true;
    public bool IncludeHeadersResponse
    {
        get => _includeHeadersResponse;
        set => this.RaiseAndSetIfChanged(ref _includeHeadersResponse, value);
    }

    private bool _includeBodyResponse = true;
    public bool IncludeBodyResponse
    {
        get => _includeBodyResponse;
        set => this.RaiseAndSetIfChanged(ref _includeBodyResponse, value);
    }
}
public class CodeGenerator
{


    public static string EscapeStringForCSharp(string value) => CSharpFormatter.ToCSharpStringLiteral(value);

    public static string GenerateCSharpCode(LogRequestModel request, LogResponseModel response, MappingCodeGeneratorConfigViewModel config)
    {
        StringBuilder codeBuilder = new StringBuilder();

        codeBuilder.AppendLine("var mappingBuilder = new MappingBuilder();");
        codeBuilder.AppendLine("mappingBuilder");
        codeBuilder.AppendLine("    .Given(Request.Create()");
        if (config.IncludeMethod)
            codeBuilder.AppendLine($"        .UsingMethod({EscapeStringForCSharp(request.Method)})");
        if (config.IncludePath)
            codeBuilder.AppendLine($"        .WithPath({EscapeStringForCSharp(request.Path)})");

        if (config.IncludeClientIP && request.ClientIP != null)
            codeBuilder.AppendLine($"        .WithClientIP({EscapeStringForCSharp(request.ClientIP)})");

        if (config.IncludeDateTime && request.DateTime != default)
            codeBuilder.AppendLine($"        .WithDateTime({EscapeStringForCSharp(request.DateTime.ToString())})");

        if (config.IncludeAbsolutePath && request.AbsolutePath != null)
            codeBuilder.AppendLine($"        .WithAbsolutePath({EscapeStringForCSharp(request.AbsolutePath)})");

        if (config.IncludeUrl && request.Url != null)
            codeBuilder.AppendLine($"        .WithUrl({EscapeStringForCSharp(request.Url)})");

        if (config.IncludeAbsoluteUrl && request.AbsoluteUrl != null)
            codeBuilder.AppendLine($"        .WithAbsoluteUrl({EscapeStringForCSharp(request.AbsoluteUrl)})");

        if (config.IncludeProxyUrl && request.ProxyUrl != null)
            codeBuilder.AppendLine($"        .WithProxyUrl({EscapeStringForCSharp(request.ProxyUrl)})");

        if (config.IncludeQuery && request.Query != null)
        {
            foreach (var query in request.Query)
            {
                string values = string.Join(", ", query.Value.Select(x=> EscapeStringForCSharp(x)));
                codeBuilder.AppendLine($"        .WithParam({EscapeStringForCSharp(query.Key)}, {values})");
            }
        }

        if (config.IncludeHeaders && request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                string values = string.Join(", ", header.Value.Select(x=> EscapeStringForCSharp(x)));
                codeBuilder.AppendLine($"        .WithHeader({EscapeStringForCSharp(header.Key)}, {values})");
            }
        }

        if (config.IncludeCookies && request.Cookies != null)
        {
            foreach (var cookie in request.Cookies)
                codeBuilder.AppendLine($"        .WithCookie({EscapeStringForCSharp(cookie.Key)}, {EscapeStringForCSharp(cookie.Value)})");
        }

        if (config.IncludeBody)
        {
            if ((request.Body) is {} body)
            {
                try
                {
                    var parsedJson = JToken.Parse(body);
                    codeBuilder.AppendLine($"        .WithBodyAsJson({CSharpFormatter.ConvertJsonToAnonymousObjectDefinition(parsedJson,2)})");
                }
                catch (Exception e)
                {
                    string escapedBody = EscapeStringForCSharp(body.ToString());
                    codeBuilder.AppendLine($"        .WithBody({escapedBody})"); 
                }
                
                  
            }else if (request.BodyAsJson is JToken bodyAsJson)
            {
                codeBuilder.AppendLine($"        .WithBodyAsJson({CSharpFormatter.ConvertJsonToAnonymousObjectDefinition(bodyAsJson, 2)})");
            }
        }

        codeBuilder.AppendLine($"    )");
        codeBuilder.AppendLine("    .RespondWith(Response.Create()");
        if (config.IncludeStatusCode && response.StatusCode != null)
            codeBuilder.AppendLine($"        .WithStatusCode({response.StatusCode})");

        if (config.IncludeHeadersResponse && response.Headers != null)
        {
            foreach (var header in response.Headers)
            {
                string values = string.Join(", ", header.Value.Select(x=> EscapeStringForCSharp(x)));
                codeBuilder.AppendLine($"        .WithHeader({EscapeStringForCSharp(header.Key)}, {values})");
            }
        }

        if (config.IncludeBodyResponse)
        {
            if ((response.Body) is {} body)
            {
                try
                {
                    var parsedJson = JToken.Parse(body);
                    codeBuilder.AppendLine($"        .WithBodyAsJson({CSharpFormatter.ConvertJsonToAnonymousObjectDefinition(parsedJson,2)})");
                }
                catch (Exception e)
                {
                    string escapedBody = EscapeStringForCSharp(body.ToString());
                    codeBuilder.AppendLine($"        .WithBody({escapedBody})"); 
                }
                
                  
            }else if (response.BodyAsJson is JToken bodyAsJson)
            {
                codeBuilder.AppendLine($"        .WithBodyAsJson({CSharpFormatter.ConvertJsonToAnonymousObjectDefinition(bodyAsJson, 2)})");
            }
            
        }

        codeBuilder.AppendLine($"    );");

        return codeBuilder.ToString();
    }

}


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