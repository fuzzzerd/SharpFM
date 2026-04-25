using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Draws a thin vertical rail in the gutter of each multi-line script
/// step, anchored to the column just after the step's opening <c>[</c>.
/// Visually links the continuation lines of a multi-line calculation
/// back to their parent step, giving the editor a "this is one logical
/// thing" cue without modifying document text.
/// </summary>
[ExcludeFromCodeCoverage]
public class ContinuationLineRenderer : IBackgroundRenderer
{
    private readonly TextArea _textArea;

    public ContinuationLineRenderer(TextArea textArea)
    {
        _textArea = textArea;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var doc = _textArea.Document;
        if (doc == null) return;

        var ranges = CachedMultiLineRanges.Compute(doc);
        var charWidth = textView.WideSpaceWidth;

        foreach (var (startLine, endLine) in ranges)
        {
            if (startLine == endLine) continue;

            var firstLine = doc.GetLineByNumber(startLine);
            var firstLineText = doc.GetText(firstLine.Offset, firstLine.Length);
            var col = MultiLineStatementRanges.FindContinuationColumn(firstLineText);
            if (col < 0) continue;

            // Monospace font is used for the script editor (Cascadia Code,
            // Consolas, Menlo) so column × char-width is correct. Subtract
            // horizontal offset for scrolling. Drawing context is already
            // in text-view coordinates.
            var x = col * charWidth - textView.HorizontalOffset;

            // Draw the rail across continuation lines (startLine+1 .. endLine).
            for (int lineNum = startLine + 1; lineNum <= endLine; lineNum++)
            {
                var visualLine = textView.GetVisualLine(lineNum);
                if (visualLine == null) continue;

                var y1 = visualLine.VisualTop - textView.VerticalOffset;
                var y2 = y1 + visualLine.Height;

                drawingContext.DrawLine(
                    ScriptEditorTheme.ContinuationRailPen,
                    new Point(x, y1),
                    new Point(x, y2));
            }
        }
    }
}
