using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Draws a thin vertical rail in the gutter of each multi-line script
/// step, anchored to the column just after the step's opening <c>[</c>.
/// Has no caret-driven state — depends only on the document's
/// statement ranges (cached on <see cref="RenderContext"/>).
/// </summary>
internal sealed class ContinuationRailLayer : IRenderLayer
{
    public KnownLayer TargetLayer => KnownLayer.Background;
    public RenderCadence Cadence => RenderCadence.Realtime;

    public bool OnCaretChanged(RenderContext ctx) => false;
    public bool OnTextChanged(RenderContext ctx) => false;

    public void Draw(RenderContext ctx, TextView textView, DrawingContext dc)
    {
        var doc = ctx.Document;
        if (doc == null) return;

        var ranges = ctx.StatementRanges;
        var charWidth = textView.WideSpaceWidth;

        foreach (var (startLine, endLine) in ranges)
        {
            if (startLine == endLine) continue;

            var firstLine = doc.GetLineByNumber(startLine);
            var firstLineText = doc.GetText(firstLine.Offset, firstLine.Length);
            var col = MultiLineStatementRanges.FindContinuationColumn(firstLineText);
            if (col < 0) continue;

            var x = col * charWidth - textView.HorizontalOffset;

            for (int lineNum = startLine + 1; lineNum <= endLine; lineNum++)
            {
                var visualLine = textView.GetVisualLine(lineNum);
                if (visualLine == null) continue;

                var y1 = visualLine.VisualTop - textView.VerticalOffset;
                var y2 = y1 + visualLine.Height;

                dc.DrawLine(
                    ScriptEditorTheme.ContinuationRailPen,
                    new Point(x, y1),
                    new Point(x, y2));
            }
        }
    }
}
