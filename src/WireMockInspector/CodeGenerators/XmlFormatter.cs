using System;
using System.Globalization;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace WireMockInspector.CodeGenerators;

internal static class XmlFormatter
{
    public static string EscapeXmlValue(string value)
    {
        // Escapes special XML characters in a string value
        return SecurityElement.Escape(value);
    }

    public static string PrettyPrintXml(string xml)
    {
        try
        {
            var xDocument = XDocument.Parse(xml);
            return xDocument.ToString();
        }
        catch (Exception)
        {
            // If parsing fails, return the original XML string
            return xml;
        }
    }

    public static string ToXmlStringLiteral(string value)
    {
        // Converts a string to a C# literal suitable for XML content
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var stringBuilder = new StringBuilder(value.Length + 2);
        stringBuilder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': stringBuilder.Append(@"\\"); break;
                case '\"': stringBuilder.Append("\\\""); break;
                case '\0': stringBuilder.Append(@"\0"); break;
                case '\a': stringBuilder.Append(@"\a"); break;
                case '\b': stringBuilder.Append(@"\b"); break;
                case '\f': stringBuilder.Append(@"\f"); break;
                case '\n': stringBuilder.Append(@"\n"); break;
                case '\r': stringBuilder.Append(@"\r"); break;
                case '\t': stringBuilder.Append(@"\t"); break;
                case '\v': stringBuilder.Append(@"\v"); break;
                default:
                    if (char.IsControl(ch) || char.IsHighSurrogate(ch) || char.IsLowSurrogate(ch))
                    {
                        stringBuilder.Append(@"\u");
                        stringBuilder.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        stringBuilder.Append(ch);
                    }
                    break;
            }
        }
        stringBuilder.Append('"');
        return stringBuilder.ToString();
    }
}
