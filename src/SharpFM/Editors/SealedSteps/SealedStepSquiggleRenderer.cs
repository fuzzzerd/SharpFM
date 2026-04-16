using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Scripting.Editor;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Draws a yellow zigzag underline across sealed-step lines — visual
/// cue that the line is read-only and editable only through the raw
/// XML popup. Uses the same geometry pattern as
/// <see cref="ErrorMarkerRenderer"/> but in a warning-gold brush.
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepSquiggleRenderer : IBackgroundRenderer
{
    private readonly TextArea _textArea;
    private readonly ScriptClipEditor _editor;

    public SealedStepSquiggleRenderer(TextArea textArea, ScriptClipEditor editor)
    {
        _textArea = textArea;
        _editor = editor;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        var doc = _textArea.Document;
        if (doc == null) return;

        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            if (anchor.Offset < 0 || anchor.Offset > doc.TextLength) continue;

            var line = doc.GetLineByOffset(anchor.Offset);
            var segment = new TextSegment
            {
                StartOffset = line.Offset,
                EndOffset = line.EndOffset
            };

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                DrawZigzag(drawingContext, rect.BottomLeft, rect.BottomRight);
            }
        }
    }

    private static void DrawZigzag(DrawingContext ctx, Point left, Point right)
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
        ctx.DrawGeometry(null, ScriptEditorTheme.SealedSquigglePen, geometry);
    }
}
