using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Draws zigzag underlines for validator diagnostics. Diagnostics live
/// on the shared <see cref="RenderContext"/>; the validator pushes them
/// in via the pipeline's <c>UpdateDiagnostics</c> method.
/// </summary>
internal sealed class ErrorMarkerLayer : IRenderLayer
{
    public KnownLayer TargetLayer => KnownLayer.Selection;
    // Diagnostics list is pushed in via the pipeline's UpdateDiagnostics
    // path (the validator runs on its own debounce). Neither caret moves
    // nor raw TextChanged need to recompute anything here.
    public RenderCadence Cadence => RenderCadence.Realtime;

    public bool OnCaretChanged(RenderContext ctx) => false;
    public bool OnTextChanged(RenderContext ctx) => false;

    /// <summary>
    /// Find the diagnostic at <paramref name="offset"/>, or null. Used
    /// by the controller to populate hover tooltips. Reads the
    /// canonical diagnostic list from the context.
    /// </summary>
    public ScriptDiagnostic? GetDiagnosticAtOffset(RenderContext ctx, int offset)
    {
        var doc = ctx.Document;
        var diagnostics = ctx.Diagnostics;
        if (doc == null || diagnostics.Count == 0) return null;
        if (offset < 0 || offset >= doc.TextLength) return null;

        var location = doc.GetLocation(offset);
        var lineIndex = location.Line - 1;

        foreach (var diag in diagnostics)
        {
            if (diag.Line != lineIndex) continue;
            var col = location.Column - 1;
            if (diag.StartCol >= diag.EndCol) return diag;
            if (col >= diag.StartCol && col <= diag.EndCol) return diag;
        }
        return null;
    }

    public void Draw(RenderContext ctx, TextView textView, DrawingContext dc)
    {
        var doc = ctx.Document;
        var diagnostics = ctx.Diagnostics;
        if (doc == null || diagnostics.Count == 0) return;

        foreach (var diag in diagnostics)
        {
            if (diag.Line < 0 || diag.Line >= doc.LineCount) continue;

            var docLine = doc.GetLineByNumber(diag.Line + 1);
            var startOffset = docLine.Offset + Math.Min(diag.StartCol, docLine.Length);
            var endOffset = docLine.Offset + Math.Min(diag.EndCol, docLine.Length);

            if (startOffset >= endOffset)
            {
                startOffset = docLine.Offset;
                endOffset = docLine.EndOffset;
            }

            var segment = new TextSegment { StartOffset = startOffset, EndOffset = endOffset };
            var pen = diag.Severity == DiagnosticSeverity.Error
                ? ScriptEditorTheme.ErrorPen
                : ScriptEditorTheme.WarningPen;

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                DrawZigzag(dc, pen, rect);
        }
    }

    private static void DrawZigzag(DrawingContext dc, IPen pen, Rect rect)
    {
        const double zigLength = 3;
        const double zigHeight = 2;

        var y = rect.Bottom;
        var startX = rect.Left;
        var endX = rect.Right;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(startX, y), false);
            bool up = true;
            for (double x = startX + zigLength; x <= endX; x += zigLength)
            {
                ctx.LineTo(new Point(x, up ? y - zigHeight : y));
                up = !up;
            }
        }

        dc.DrawGeometry(null, pen, geometry);
    }
}
