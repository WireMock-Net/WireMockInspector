using Fluid;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluid.Values;

namespace WireMockInspector.Templates
{

    class FluidTemplateEngine 
    {
        public string Transform(Template template, object? context)
        {
            var options = new TemplateOptions();
            options.Filters.AddFilter("toAnonymousObject", (input, arguments, templateContext) =>
            {
                return new StringValue(Map(input, templateContext));
            });
            var parser = new FluidParser();
            if (parser.TryParse(template.Content, out var ftemplate, out var error))
            {
                var result = ftemplate.Render(new TemplateContext(context, options));
                return result;
            }

            throw new InvalidOperationException(error);
        }

        private static string Map(FluidValue input, TemplateContext templateContext, int ind = 0)
        {
            
            return input switch
            {
                ArrayValue arrayValue => "["+string.Join(", ", arrayValue.Values.Select(input1 => Map(input1, templateContext, ind)))+"]",
                BlankValue blankValue => "_",
                BooleanValue booleanValue => booleanValue.ToStringValue(),
                DateTimeValue dateTimeValue => "\""+dateTimeValue.ToStringValue()+"\"",
                DictionaryValue dictionaryValue => "new \r\n"+ new string(' ', 4 * ind) + "{\r\n " + string.Join(",\r\n", dictionaryValue.Enumerate(templateContext).OfType<ArrayValue>().Select(input1 => $"{new string(' ', 4 * (ind+1))}{input1.Values[0].ToStringValue()} = { Map(input1.Values[1], templateContext, ind +1)}")) +"\r\n"+ new string(' ', 4 * ind) + "}",
                EmptyValue emptyValue => "null",
                NilValue nilValue => "null",
                NumberValue numberValue => numberValue.ToStringValue(),
                ObjectValue objectValue => objectValue.ToStringValue(),
                ObjectValueBase objectValueBase => objectValueBase.ToStringValue(),
                StringValue stringValue => $"\"{stringValue.ToStringValue()}\"",
                _ => throw new ArgumentOutOfRangeException(nameof(input))
            };
        }
    }

    public class Template
    {
        public string Content { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileName => Path.GetFileName(FilePath);
    }
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

        public object? Read(Source source)
        {
            var json = JToken.Parse(source.Content);

            if (json is JObject jo && jo.ContainsKey("$schema"))
            {
                jo.Remove("$schema");
            }

            return ConvertJsonToObject(json);
        }
    }

    internal class Source
    {
        public string Path { get; set; } = null!;
        public string Content { get; set; } = null!;
        public Dictionary<string, string> SourceMetadata { get; set; } = null!;
    }

}
