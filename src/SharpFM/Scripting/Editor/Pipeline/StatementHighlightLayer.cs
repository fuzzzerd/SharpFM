using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Highlights the step containing the caret with a subtle background
/// rectangle. Reads cached statement ranges from the
/// <see cref="RenderContext"/>; only updates when the caret crosses
/// a physical line, since the highlighted step doesn't change for
/// in-line caret movement.
/// </summary>
internal sealed class StatementHighlightLayer : IRenderLayer
{
    public KnownLayer TargetLayer => KnownLayer.Background;

    private int _highlightStartLine = -1;
    private int _highlightEndLine = -1;
    private int _lastCaretLine = -1;

    public bool OnCaretChanged(RenderContext ctx)
    {
        var caretLine = ctx.CaretLine;
        if (caretLine == _lastCaretLine) return false;
        _lastCaretLine = caretLine;

        var oldStart = _highlightStartLine;
        var oldEnd = _highlightEndLine;
        _highlightStartLine = -1;
        _highlightEndLine = -1;

        var doc = ctx.Document;
        if (doc == null) return oldStart != -1 || oldEnd != -1;

        var ranges = ctx.StatementRanges;
        var hit = MultiLineStatementRanges.FindRangeContainingLine(ranges, caretLine);
        if (hit is { } range)
        {
            var firstLineText = doc.GetText(doc.GetLineByNumber(range.StartLine));
            if (!string.IsNullOrWhiteSpace(firstLineText))
            {
                _highlightStartLine = range.StartLine;
                _highlightEndLine = range.EndLine;
            }
        }

        return _highlightStartLine != oldStart || _highlightEndLine != oldEnd;
    }

    public void Draw(RenderContext ctx, TextView textView, DrawingContext dc)
    {
        if (_highlightStartLine < 0 || _highlightEndLine < 0) return;
        var doc = ctx.Document;
        if (doc == null) return;

        var startLine = doc.GetLineByNumber(_highlightStartLine);
        var endLine = doc.GetLineByNumber(_highlightEndLine);
        var segment = new TextSegment
        {
            StartOffset = startLine.Offset,
            EndOffset = endLine.EndOffset,
        };

        var builder = new BackgroundGeometryBuilder { CornerRadius = 2 };
        builder.AddSegment(textView, segment);
        var geometry = builder.CreateGeometry();
        if (geometry != null)
            dc.DrawGeometry(ScriptEditorTheme.StatementHighlightBrush, null, geometry);
    }
}
