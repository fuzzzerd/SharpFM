using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor;

public class BracketMatchRenderer : IBackgroundRenderer
{
    private static readonly IBrush MatchBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    private static readonly IPen MatchPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0);

    private readonly TextArea _textArea;
    private int _openOffset = -1;
    private int _closeOffset = -1;

    public BracketMatchRenderer(TextArea textArea)
    {
        _textArea = textArea;
        _textArea.Caret.PositionChanged += (_, _) => UpdateBracketMatch();
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void UpdateBracketMatch()
    {
        var oldOpen = _openOffset;
        var oldClose = _closeOffset;
        _openOffset = -1;
        _closeOffset = -1;

        var doc = _textArea.Document;
        if (doc == null) return;

        var offset = _textArea.Caret.Offset;
        if (offset <= 0 || offset > doc.TextLength) return;

        // Check character before caret and at caret
        var text = doc.Text;
        var charBefore = offset > 0 ? doc.GetCharAt(offset - 1) : '\0';
        var charAt = offset < doc.TextLength ? doc.GetCharAt(offset) : '\0';

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

        if (_openOffset != oldOpen || _closeOffset != oldClose)
            _textArea.TextView.InvalidateLayer(Layer);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_openOffset < 0 || _closeOffset < 0) return;

        DrawBracketHighlight(textView, drawingContext, _openOffset);
        DrawBracketHighlight(textView, drawingContext, _closeOffset);
    }

    private static void DrawBracketHighlight(TextView textView, DrawingContext context, int offset)
    {
        var segment = new TextSegment { StartOffset = offset, EndOffset = offset + 1 };
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            context.DrawRectangle(MatchBrush, MatchPen, rect);
        }
    }

}
