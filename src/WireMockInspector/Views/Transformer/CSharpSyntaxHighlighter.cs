using System;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace WireMockInspector.Views.Transformer;

public class CSharpSyntaxHighlighter : DocumentColorizingTransformer
{
    // List of C# keywords to highlight
    private static readonly string[] Keywords = new string[]
    {
        "var", "public", "private", "protected", "class", "void", "int", "string", "bool", "new", "namespace", "using"
    };

    // Matches method calls like `.UsingMethod("....")`
    private static readonly Regex MethodCallRegex = new Regex(@"\.(\w+)\(", RegexOptions.Compiled);

    // Matches string literals
    private static readonly Regex StringLiteralRegex = new Regex(@"""(\\.|[^\\\""])*""", RegexOptions.Compiled);

    // Custom list for additional class names to be highlighted
    private static readonly string[] CustomClasses = new string[]
    {
        "MappingBuilder", "Request", "Response"
    };

    // Colors
    private static readonly SolidColorBrush KeywordBrush = new SolidColorBrush(Colors.Blue);
    private static readonly SolidColorBrush MethodBrush = new SolidColorBrush(Colors.Yellow);
    private static readonly SolidColorBrush StringLiteralBrush = new SolidColorBrush(Colors.LightBlue);
    private static readonly SolidColorBrush ClassNameBrush = new SolidColorBrush(Colors.Teal);

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineText = CurrentContext.Document.GetText(line);
        int lineStartOffset = line.Offset;

        // Highlight keywords
        foreach (var keyword in Keywords)
        {
            HighlightWord(lineText, lineStartOffset, keyword, KeywordBrush);
        }

        // Highlight method calls
        foreach (Match match in MethodCallRegex.Matches(lineText))
        {
            ChangeLinePart(
                lineStartOffset + match.Index + 1, // Start offset (skip initial '.')
                lineStartOffset + match.Index + match.Length - 1, // End offset (skip '(')
                element => element.TextRunProperties.SetForegroundBrush(MethodBrush));
        }

        // Highlight string literals
        foreach (Match match in StringLiteralRegex.Matches(lineText))
        {
            ChangeLinePart(
                lineStartOffset + match.Index,
                lineStartOffset + match.Index + match.Length,
                element => element.TextRunProperties.SetForegroundBrush(StringLiteralBrush));
        }

        // Highlight custom class names
        foreach (var className in CustomClasses)
        {
            HighlightWord(lineText, lineStartOffset, className, ClassNameBrush);
        }
    }

    private void HighlightWord(string text, int offset, string word, SolidColorBrush brush)
    {
        int start = 0;
        while ((start = text.IndexOf(word, start, StringComparison.Ordinal)) >= 0)
        {
            if ((start == 0 || !char.IsLetterOrDigit(text[start - 1])) &&
                (start + word.Length == text.Length || !char.IsLetterOrDigit(text[start + word.Length])))
            {
                ChangeLinePart(
                    offset + start, // Start offset
                    offset + start + word.Length, // End offset
                    element => element.TextRunProperties.SetForegroundBrush(brush));
            }
            start += word.Length;
        }
    }
}