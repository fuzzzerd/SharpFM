using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Highlights the matching <c>[</c>/<c>]</c> pair when the caret sits
/// adjacent to one of them. Updates only when the caret is near a
/// bracket — for all other caret moves the layer reports clean and
/// no Avalonia work is triggered.
/// </summary>
internal sealed class BracketMatchLayer : IRenderLayer
{
    public KnownLayer TargetLayer => KnownLayer.Selection;

    private int _openOffset = -1;
    private int _closeOffset = -1;

    public bool OnCaretChanged(RenderContext ctx)
    {
        var oldOpen = _openOffset;
        var oldClose = _closeOffset;
        _openOffset = -1;
        _closeOffset = -1;

        var doc = ctx.Document;
        if (doc == null) return oldOpen != -1 || oldClose != -1;

        var offset = ctx.CaretOffset;
        if (offset <= 0 || offset > doc.TextLength)
            return oldOpen != -1 || oldClose != -1;

        // Defer reading doc.Text (full-document allocation) until we know
        // we're actually adjacent to a bracket — most caret positions
        // aren't, and this fires per keystroke + arrow key.
        var charBefore = offset > 0 ? doc.GetCharAt(offset - 1) : '\0';
        var charAt = offset < doc.TextLength ? doc.GetCharAt(offset) : '\0';

        if (charBefore != '[' && charBefore != ']' && charAt != '[' && charAt != ']')
            return oldOpen != -1 || oldClose != -1;

        var text = doc.Text;

        if (charBefore == '[')
        {
            var match = BracketMatcher.FindMatchingClose(text, offset - 1);
            if (match >= 0) { _openOffset = offset - 1; _closeOffset = match; }
        }
        else if (charBefore == ']')
        {
            var match = BracketMatcher.FindMatchingOpen(text, offset - 2);
            if (match >= 0) { _openOffset = match; _closeOffset = offset - 1; }
        }
        else if (charAt == '[')
        {
            var match = BracketMatcher.FindMatchingClose(text, offset);
            if (match >= 0) { _openOffset = offset; _closeOffset = match; }
        }
        else if (charAt == ']')
        {
            var match = BracketMatcher.FindMatchingOpen(text, offset - 1);
            if (match >= 0) { _openOffset = match; _closeOffset = offset; }
        }

        return _openOffset != oldOpen || _closeOffset != oldClose;
    }

    public void Draw(RenderContext ctx, TextView textView, DrawingContext dc)
    {
        if (_openOffset < 0 || _closeOffset < 0) return;
        DrawBracketHighlight(textView, dc, _openOffset);
        DrawBracketHighlight(textView, dc, _closeOffset);
    }

    private static void DrawBracketHighlight(TextView textView, DrawingContext dc, int offset)
    {
        var segment = new TextSegment { StartOffset = offset, EndOffset = offset + 1 };
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            dc.DrawRectangle(ScriptEditorTheme.BracketMatchBrush, ScriptEditorTheme.BracketMatchPen, rect);
        }
    }
}
