using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor;

public class ErrorMarkerRenderer : IBackgroundRenderer
{
    private readonly TextDocument _document;
    private List<ScriptDiagnostic> _diagnostics = new();


    public ErrorMarkerRenderer(TextDocument document)
    {
        _document = document;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void UpdateDiagnostics(List<ScriptDiagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public ScriptDiagnostic? GetDiagnosticAtOffset(int offset)
    {
        if (_diagnostics.Count == 0 || offset < 0 || offset >= _document.TextLength)
            return null;

        var location = _document.GetLocation(offset);
        var lineIndex = location.Line - 1; // 1-indexed to 0-indexed

        foreach (var diag in _diagnostics)
        {
            if (diag.Line != lineIndex) continue;

            var col = location.Column - 1; // 1-indexed to 0-indexed
            var startCol = diag.StartCol;
            var endCol = diag.EndCol;

            // If no specific span, the whole line is the target
            if (startCol >= endCol)
            {
                return diag;
            }

            if (col >= startCol && col <= endCol)
                return diag;
        }

        return null;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_diagnostics.Count == 0) return;

        foreach (var diag in _diagnostics)
        {
            if (diag.Line < 0 || diag.Line >= _document.LineCount)
                continue;

            var docLine = _document.GetLineByNumber(diag.Line + 1); // 0-indexed to 1-indexed
            var startOffset = docLine.Offset + Math.Min(diag.StartCol, docLine.Length);
            var endOffset = docLine.Offset + Math.Min(diag.EndCol, docLine.Length);

            if (startOffset >= endOffset)
            {
                // If no span, underline the whole line content
                startOffset = docLine.Offset;
                endOffset = docLine.EndOffset;
            }

            var segment = new TextSegment { StartOffset = startOffset, EndOffset = endOffset };
            var pen = diag.Severity == DiagnosticSeverity.Error
                ? ScriptEditorTheme.ErrorPen
                : ScriptEditorTheme.WarningPen;

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                DrawZigzag(drawingContext, pen, rect);
            }
        }
    }

    private static void DrawZigzag(DrawingContext context, IPen pen, Rect rect)
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

        context.DrawGeometry(null, pen, geometry);
    }
}
