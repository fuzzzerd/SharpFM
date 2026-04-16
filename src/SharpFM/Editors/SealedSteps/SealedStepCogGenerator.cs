using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Inline element generator that places a small clickable cog button
/// at the end of every sealed-step line. Clicking the cog raises the
/// <see cref="CogClicked"/> event with the sealed step's line anchor;
/// the subscriber (ScriptEditorController) opens the raw-XML editor
/// dialog and writes the result back to the sealed-step cache.
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepCogGenerator : VisualLineElementGenerator
{
    private readonly ScriptClipEditor _editor;

    public event EventHandler<TextAnchor>? CogClicked;

    public SealedStepCogGenerator(ScriptClipEditor editor)
    {
        _editor = editor;
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        // The cog sits at the end of each sealed line. Return the
        // earliest end-of-line offset that belongs to a sealed anchor
        // and is >= startOffset; -1 when no more.
        int best = int.MaxValue;
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            var line = CurrentContext.Document.GetLineByOffset(anchor.Offset);
            if (line.EndOffset >= startOffset && line.EndOffset < best)
                best = line.EndOffset;
        }
        return best == int.MaxValue ? -1 : best;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            var line = CurrentContext.Document.GetLineByOffset(anchor.Offset);
            if (line.EndOffset != offset) continue;

            var button = new Button
            {
                Content = "⚙",
                Padding = new Thickness(4, 0),
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 12,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };

            // Capture the anchor in the click handler so the subscriber
            // can map back to the sealed step's XML.
            var capturedAnchor = anchor;
            button.Click += (_, _) => CogClicked?.Invoke(this, capturedAnchor);

            return new InlineObjectElement(0, button);
        }

        return null;
    }
}
