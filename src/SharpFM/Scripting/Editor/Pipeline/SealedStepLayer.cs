using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Draws sealed-step background cues: a left-edge accent stripe across
/// every sealed line and a gold zigzag underline beneath. Reads sealed
/// line numbers from the shared <see cref="RenderContext"/> snapshot
/// (no per-line anchor walks, no per-paint string allocations) and
/// recomputes only on idle ticks driven by <c>Document.TextChanged</c>.
///
/// Replaces the standalone <c>SealedStepSquiggleRenderer</c> and lifts
/// its work into the pipeline so a single <c>InvalidateLayer</c> call
/// can collapse multiple feature changes into one repaint.
/// </summary>
internal sealed class SealedStepLayer : IRenderLayer
{
    public KnownLayer TargetLayer => KnownLayer.Selection;
    public RenderCadence Cadence => RenderCadence.Idle;

    // Snapshot of the sealed-line set captured at the last idle tick.
    // Compared on the next tick to decide whether the layer needs to
    // repaint at all.
    private int[] _lastLineSet = System.Array.Empty<int>();

    public bool OnCaretChanged(RenderContext ctx) => false;

    public bool OnTextChanged(RenderContext ctx)
    {
        var current = ctx.SealedLineNumbers;
        if (current.Count == _lastLineSet.Length)
        {
            var allSame = true;
            foreach (var n in _lastLineSet)
            {
                if (!current.Contains(n)) { allSame = false; break; }
            }
            if (allSame) return false;
        }

        var snapshot = new int[current.Count];
        var i = 0;
        foreach (var n in current) snapshot[i++] = n;
        _lastLineSet = snapshot;
        return true;
    }

    public void Draw(RenderContext ctx, TextView textView, DrawingContext dc)
    {
        var doc = ctx.Document;
        if (doc == null) return;

        var sealedLines = ctx.SealedLineNumbers;
        if (sealedLines.Count == 0) return;

        foreach (var lineNumber in sealedLines)
        {
            if (lineNumber < 1 || lineNumber > doc.LineCount) continue;
            var line = doc.GetLineByNumber(lineNumber);
            var segment = new TextSegment
            {
                StartOffset = line.Offset,
                EndOffset = line.EndOffset,
            };

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                DrawLeftStripe(dc, rect);
                DrawZigzag(dc, rect.BottomLeft, rect.BottomRight);
            }
        }
    }

    private static void DrawLeftStripe(DrawingContext dc, Rect rect)
    {
        var stripe = new Rect(
            rect.X,
            rect.Y,
            ScriptEditorTheme.SealedLeftStripeWidth,
            rect.Height);
        dc.DrawRectangle(ScriptEditorTheme.SealedLeftStripeBrush, null, stripe);
    }

    private static void DrawZigzag(DrawingContext dc, Point left, Point right)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const double amp = 1.5;
            const double period = 4.0;
            double x = left.X;
            double baseY = left.Y - 1;
            g.BeginFigure(new Point(x, baseY), false);
            bool up = true;
            while (x < right.X)
            {
                x += period / 2;
                var y = up ? baseY - amp : baseY + amp;
                g.LineTo(new Point(x, y));
                up = !up;
            }
        }
        dc.DrawGeometry(null, ScriptEditorTheme.SealedSquigglePen, geometry);
    }
}
