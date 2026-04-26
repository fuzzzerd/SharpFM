using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Inline element generator that places a small clickable cog button
/// at the end of every sealed-step line. Reads the cached
/// <see cref="ScriptClipEditor.SealedLineEndOffsets"/> map for O(1)
/// lookup — no per-call iteration over the anchor dictionary, no
/// per-anchor <c>Document.GetText</c> string allocations.
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
        if (!_editor.HasSealedAnchors) return -1;

        var endOffsets = _editor.SealedLineEndOffsets;
        if (endOffsets.Count == 0) return -1;

        int best = int.MaxValue;
        foreach (var end in endOffsets.Values)
        {
            if (end >= startOffset && end < best)
                best = end;
        }
        return best == int.MaxValue ? -1 : best;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        if (!_editor.HasSealedAnchors) return null;
        var endOffsets = _editor.SealedLineEndOffsets;

        // Verify offset matches a sealed line's end. Don't allocate a
        // button for non-sealed line ends.
        var matched = false;
        foreach (var end in endOffsets.Values)
        {
            if (end == offset) { matched = true; break; }
        }
        if (!matched) return null;

        // Resolve which anchor lives on this line so the click handler
        // can map back to the sealed step's XML. Iterating SealedAnchors
        // is acceptable here (called once per sealed line construction,
        // not per visible line per layout). Use a stable strategy: pick
        // the anchor whose line contains the offset.
        var doc = CurrentContext.Document;
        TextAnchor? hit = null;
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            if (anchor.Offset < 0 || anchor.Offset > doc.TextLength) continue;
            var line = doc.GetLineByOffset(anchor.Offset);
            if (line.EndOffset == offset) { hit = anchor; break; }
        }
        if (hit == null) return null;

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

        var capturedAnchor = hit;
        button.Click += (_, _) => CogClicked?.Invoke(this, capturedAnchor);

        return new InlineObjectElement(0, button);
    }
}
