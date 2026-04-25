using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor;

[ExcludeFromCodeCoverage]
public class StatementHighlightRenderer : IBackgroundRenderer
{

    private readonly TextArea _textArea;
    private int _highlightStartLine = -1;
    private int _highlightEndLine = -1;
    private int _lastCaretLine = -1;

    public StatementHighlightRenderer(TextArea textArea)
    {
        _textArea = textArea;
        _textArea.Caret.PositionChanged += (_, _) => UpdateHighlight();
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void UpdateHighlight()
    {
        var doc = _textArea.Document;
        if (doc == null) return;

        var caretLine = _textArea.Caret.Line; // 1-indexed

        // PositionChanged fires on every keystroke and every arrow key —
        // but the highlighted *step* only changes when the caret moves
        // to a different physical line. Skip the recompute for in-line
        // movement (which is the vast majority of caret events).
        if (caretLine == _lastCaretLine) return;
        _lastCaretLine = caretLine;

        var oldStart = _highlightStartLine;
        var oldEnd = _highlightEndLine;
        _highlightStartLine = -1;
        _highlightEndLine = -1;

        var ranges = CachedMultiLineRanges.Compute(doc);

        // Highlight the step the caret is in — single-line OR multi-line.
        // Skip blank-line "ranges" so the highlight doesn't appear on
        // empty lines between steps.
        var hit = MultiLineStatementRanges.FindRangeContainingLine(ranges, caretLine);
        if (hit is { } range)
        {
            var lineText = doc.GetText(doc.GetLineByNumber(range.StartLine));
            if (!string.IsNullOrWhiteSpace(lineText))
            {
                _highlightStartLine = range.StartLine;
                _highlightEndLine = range.EndLine;
            }
        }

        if (_highlightStartLine != oldStart || _highlightEndLine != oldEnd)
            _textArea.TextView.InvalidateLayer(Layer);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_highlightStartLine < 0 || _highlightEndLine < 0) return;

        var doc = _textArea.Document;
        if (doc == null) return;

        var startLine = doc.GetLineByNumber(_highlightStartLine);
        var endLine = doc.GetLineByNumber(_highlightEndLine);
        var segment = new TextSegment
        {
            StartOffset = startLine.Offset,
            EndOffset = endLine.EndOffset
        };

        var builder = new BackgroundGeometryBuilder { CornerRadius = 2 };
        builder.AddSegment(textView, segment);
        var geometry = builder.CreateGeometry();
        if (geometry != null)
        {
            drawingContext.DrawGeometry(ScriptEditorTheme.StatementHighlightBrush, null, geometry);
        }
    }

}
