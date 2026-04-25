using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
namespace SharpFM.Scripting.Editor;

/// <summary>
/// Custom margin that displays FileMaker-style step indices: one number
/// per script step regardless of how many physical lines a multi-line
/// calc spans. Continuation lines render no number — matching FM Pro's
/// convention that each step counts as a single "line" in the script.
/// <para>
/// Backed by <see cref="MultiLineStatementRanges.BuildStepIndex"/>; the
/// lookup is rebuilt whenever the document text version changes.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public class StepIndexMargin : AbstractMargin
{
    private IReadOnlyDictionary<int, int> _stepIndex = new Dictionary<int, int>();
    private Typeface _typeface = new(FontFamily.Default);
    private double _emSize = 12;

    // FormattedText is heavy to construct (font shaping + glyph layout).
    // Step numbers are bounded — for the visible range, they're a tiny
    // recurring set ("1", "2", "3", …) — so cache by string. Reset when
    // the typeface or em size changes (handled in OnTextViewChanged).
    private readonly Dictionary<string, FormattedText> _formattedCache = new();

    // Track the last visible line range + step-index identity we
    // actually rendered. VisualLinesChanged fires for many reasons
    // (every layout pass, scroll attempts, font property changes, etc.)
    // — when neither the visible-line range nor the step-index dict
    // identity has changed since our last paint, the next paint would
    // be pixel-identical, so InvalidateVisual is wasted work.
    private int _lastFirstVisibleLine = -1;
    private int _lastLastVisibleLine = -1;
    private object? _lastStepIndexKey;

    public StepIndexMargin()
    {
        // Match the editor's default font; will be re-pulled on attach.
    }

    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        if (oldTextView != null)
        {
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        }

        if (newTextView != null)
        {
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
            _typeface = new Typeface(newTextView.GetValue(TextElement.FontFamilyProperty));
            _emSize = newTextView.GetValue(TextElement.FontSizeProperty);
            _formattedCache.Clear();
        }

        base.OnTextViewChanged(oldTextView, newTextView);
        InvalidateMeasure();
    }

    private void OnVisualLinesChanged(object? sender, System.EventArgs e)
    {
        var tv = TextView;
        if (tv == null || !tv.VisualLinesValid)
        {
            InvalidateVisual();
            return;
        }

        var visualLines = tv.VisualLines;
        if (visualLines.Count == 0)
        {
            InvalidateVisual();
            return;
        }

        var first = visualLines[0].FirstDocumentLine.LineNumber;
        var last = visualLines[visualLines.Count - 1].FirstDocumentLine.LineNumber;
        var doc = tv.Document;
        // Reference-identity on the cached step-index dict — same
        // instance means the document version hasn't changed since
        // CachedMultiLineRanges last computed.
        object? stepIndexKey = doc != null
            ? CachedMultiLineRanges.GetStepIndex(doc)
            : null;

        if (first == _lastFirstVisibleLine
            && last == _lastLastVisibleLine
            && ReferenceEquals(stepIndexKey, _lastStepIndexKey))
        {
            return;
        }

        _lastFirstVisibleLine = first;
        _lastLastVisibleLine = last;
        _lastStepIndexKey = stepIndexKey;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Reserve enough width for ~5-digit step numbers.
        var sample = MakeFormattedText("99999");
        return new Size(sample.Width + 10, 0);
    }

    public override void Render(DrawingContext context)
    {
        var textView = TextView;
        if (textView == null || !textView.VisualLinesValid) return;

        EnsureStepIndexFresh(textView.Document);

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNum = visualLine.FirstDocumentLine.LineNumber;
            if (!_stepIndex.TryGetValue(lineNum, out var stepIndex)) continue;

            var formatted = MakeFormattedText(stepIndex.ToString(CultureInfo.InvariantCulture));
            var y = visualLine.VisualTop - textView.VerticalOffset;
            var x = Bounds.Width - formatted.Width - 4;
            context.DrawText(formatted, new Point(x, y));
        }
    }

    private void EnsureStepIndexFresh(TextDocument? document)
    {
        if (document == null) return;
        // Defer to the shared cache so multiple renderers reading the
        // same document version reuse one Compute pass.
        _stepIndex = CachedMultiLineRanges.GetStepIndex(document);
    }

    private FormattedText MakeFormattedText(string text)
    {
        if (_formattedCache.TryGetValue(text, out var cached))
            return cached;
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, _typeface, _emSize, Brushes.Gray);
        _formattedCache[text] = formatted;
        return formatted;
    }
}
