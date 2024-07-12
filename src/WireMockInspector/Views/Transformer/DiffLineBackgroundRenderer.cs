using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using DiffPlex.DiffBuilder.Model;

namespace WireMockInspector.Views.Transformer;

public class DiffLineBackgroundRenderer : GenericLineTransformer
{
    private readonly List<DiffPiece>? _mdOldTextLines;

    protected override void TransformLine(DocumentLine line, ITextRunConstructionContext context)
    {
        if (_mdOldTextLines is { } li)
        {
            var index = line.LineNumber - 1;
            if (index is > -1 && index < li.Count)
            {
                var brush = li[index].Type switch
                {
                    ChangeType.Deleted => new SolidColorBrush(Colors.Red, 0.5),
                    ChangeType.Inserted => new SolidColorBrush(Colors.Green, 0.5),
                    ChangeType.Imaginary => new SolidColorBrush(Colors.Gray, 0.5),
                    ChangeType.Modified => new SolidColorBrush(Colors.Orange, 0.5),
                    _ => null
                };
                if (brush != null)
                {
                    SetTextStyle(line, 0, line.Length, null, brush,
                        context.GlobalTextRunProperties.Typeface.Style,
                        context.GlobalTextRunProperties.Typeface.Weight, false);
                }
            }
        }
    }

    public DiffLineBackgroundRenderer(List<DiffPiece>? mdOldTextLines) : base((_) => { })
    {
        _mdOldTextLines = mdOldTextLines;
    }
}