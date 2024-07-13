using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace WireMockInspector.Views.Transformer
{
    public class JsonSyntaxHighlighter : DocumentColorizingTransformer
    {
        // Regex patterns for different JSON components
        private static readonly Regex PropertyNameRegex = new(@"""[^""\\]*(?:\\.[^""\\]*)*""(?=\s*:)", RegexOptions.Compiled);
        private static readonly Regex StringLiteralRegex = new(@"""[^""\\]*(?:\\.[^""\\]*)*""(?=\s*[,}\]])(?!\s*:)", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);
        private static readonly Regex BooleanNullRegex = new(@"\b(true|false|null)\b", RegexOptions.Compiled);
        private static readonly Regex PunctuationRegex = new(@"[\[\]{}:,]", RegexOptions.Compiled);

        // Colors using the same palette as CSharpSyntaxHighlighter
        private static readonly SolidColorBrush PropertyNameBrush = new(Color.Parse("#ADD795"));
        private static readonly SolidColorBrush StringLiteralBrush = new(Color.Parse("#D6936B"));
        private static readonly SolidColorBrush NumberBrush = new(Color.Parse("#ADD795"));
        private static readonly SolidColorBrush BooleanNullBrush = new(Color.Parse("#41C2B0"));
        private static readonly SolidColorBrush PunctuationBrush = new(Color.Parse("#3988D6"));

        protected override void ColorizeLine(DocumentLine line)
        {
            var lineText = CurrentContext.Document.GetText(line);
            int lineStartOffset = line.Offset;

            // Highlight property names
            foreach (Match match in PropertyNameRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(PropertyNameBrush));
            }

            // Highlight string literals
            foreach (Match match in StringLiteralRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(StringLiteralBrush));
            }

            // Highlight numbers
            foreach (Match match in NumberRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(NumberBrush));
            }

            // Highlight boolean and null values
            foreach (Match match in BooleanNullRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(BooleanNullBrush));
            }

            // Highlight punctuation
            foreach (Match match in PunctuationRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(PunctuationBrush));
            }
        }
    }
}