using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace WireMockInspector.Views.Transformer
{
    public class XmlSyntaxHighlighter : DocumentColorizingTransformer
    {
        // Regex patterns for XML elements
        private static readonly Regex TagRegex = new(@"<(/?[\w\s-]+)(.*?)>", RegexOptions.Compiled);
        private static readonly Regex AttributeRegex = new(@"\b(\w+)(\s*=\s*)(""[^""]*"")", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new(@"<!--(.*?)-->", RegexOptions.Compiled);

        // Colors
        private static readonly SolidColorBrush TagBrush = new SolidColorBrush(Color.Parse("#ADD795"));
        private static readonly SolidColorBrush AttributeNameBrush = new SolidColorBrush(Color.Parse("#41C2B0"));
        private static readonly SolidColorBrush AttributeValueBrush = new SolidColorBrush(Color.Parse("#D6936B"));
        private static readonly SolidColorBrush CommentBrush = new SolidColorBrush(Color.Parse("#ADD795"));

        protected override void ColorizeLine(DocumentLine line)
        {
            var lineText = CurrentContext.Document.GetText(line);
            int lineStartOffset = line.Offset;

            // Highlight XML tags
            foreach (Match match in TagRegex.Matches(lineText))
            {
                // Highlight the tag name
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Groups[1].Length + 2, // Tag name
                    element => element.TextRunProperties.SetForegroundBrush(TagBrush));

                // Highlight attributes within the tag
                if (match.Groups[2].Success)
                {
                    HighlightAttributes(lineText, lineStartOffset + match.Index + match.Groups[1].Length + 1, match.Groups[2].Value);
                }
            }

            // Highlight comments
            foreach (Match match in CommentRegex.Matches(lineText))
            {
                ChangeLinePart(
                    lineStartOffset + match.Index,
                    lineStartOffset + match.Index + match.Length,
                    element => element.TextRunProperties.SetForegroundBrush(CommentBrush));
            }
        }

        private void HighlightAttributes(string lineText, int offset, string attributesText)
        {
            foreach (Match match in AttributeRegex.Matches(attributesText))
            {
                // Highlight attribute name
                ChangeLinePart(
                    offset + match.Index,
                    offset + match.Index + match.Groups[1].Length,
                    element => element.TextRunProperties.SetForegroundBrush(AttributeNameBrush));

                // Highlight attribute value
                if (match.Groups[3].Success)
                {
                    ChangeLinePart(
                        offset + match.Index + match.Groups[1].Length + match.Groups[2].Length,
                        offset + match.Index + match.Groups[1].Length + match.Groups[2].Length + match.Groups[3].Length,
                        element => element.TextRunProperties.SetForegroundBrush(AttributeValueBrush));
                }
            }
        }
    }
}
