using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor;

public class StatementHighlightRenderer : IBackgroundRenderer
{

    private readonly TextArea _textArea;
    private int _highlightStartLine = -1;
    private int _highlightEndLine = -1;

    public StatementHighlightRenderer(TextArea textArea)
    {
        _textArea = textArea;
        _textArea.Caret.PositionChanged += (_, _) => UpdateHighlight();
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void UpdateHighlight()
    {
        var oldStart = _highlightStartLine;
        var oldEnd = _highlightEndLine;
        _highlightStartLine = -1;
        _highlightEndLine = -1;

        var doc = _textArea.Document;
        if (doc == null) return;

        var caretLine = _textArea.Caret.Line; // 1-indexed
        var ranges = ComputeStatementRanges(doc.Text);

        foreach (var (start, end) in ranges)
        {
            // Only highlight multi-line statements
            if (start == end) continue;

            if (caretLine >= start && caretLine <= end)
            {
                _highlightStartLine = start;
                _highlightEndLine = end;
                break;
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

    internal static List<(int StartLine, int EndLine)> ComputeStatementRanges(string text)
    {
        var ranges = new List<(int, int)>();
        var lines = text.Split('\n');
        int currentStart = -1;
        int depth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineNum = i + 1; // 1-indexed for AvaloniaEdit

            if (currentStart < 0)
            {
                // Not in a multi-line statement
                if (BracketMatcher.HasUnbalancedBrackets(line))
                {
                    currentStart = lineNum;
                    depth = BracketMatcher.CountBracketDepth(line);
                }
                else
                {
                    ranges.Add((lineNum, lineNum));
                }
            }
            else
            {
                // Continuing a multi-line statement
                depth += BracketMatcher.CountBracketDepth(line);
                if (depth <= 0)
                {
                    ranges.Add((currentStart, lineNum));
                    currentStart = -1;
                    depth = 0;
                }
            }
        }

        // Unclosed statement at end
        if (currentStart >= 0)
        {
            ranges.Add((currentStart, lines.Length));
        }

        return ranges;
    }

}
