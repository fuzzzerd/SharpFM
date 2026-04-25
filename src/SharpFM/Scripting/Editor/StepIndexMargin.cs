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
        }

        base.OnTextViewChanged(oldTextView, newTextView);
        InvalidateMeasure();
    }

    private void OnVisualLinesChanged(object? sender, System.EventArgs e)
    {
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

    private FormattedText MakeFormattedText(string text) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            _typeface, _emSize, Brushes.Gray);
}
