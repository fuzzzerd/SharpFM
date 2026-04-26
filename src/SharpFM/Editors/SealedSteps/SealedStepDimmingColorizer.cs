using System.Diagnostics.CodeAnalysis;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SharpFM.Scripting.Editor;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Applies a dimmed foreground brush to sealed-step lines so they read
/// as "annotation / read-only summary" rather than as first-class code.
/// <para>
/// Earlier versions flipped the typeface to italic — that drove a font
/// lookup and a <c>ShapeTextRuns</c> pass per text run per layout, and
/// became the dominant cost of every keystroke. A foreground-brush
/// change is paint-time only; it does not invalidate shape state, so
/// the colorizer's per-line cost is now an O(1) hashset lookup plus
/// one <c>SetForegroundBrush</c> call per sealed line.
/// </para>
/// <para>
/// Backed by <see cref="ScriptClipEditor.SealedLineNumbers"/> — a
/// per-document cache invalidated on <c>Document.TextChanged</c> —
/// so the colorizer never iterates anchors and never allocates the
/// per-anchor signature strings the legacy implementation did.
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepDimmingColorizer : DocumentColorizingTransformer
{
    private readonly ScriptClipEditor _editor;

    public SealedStepDimmingColorizer(ScriptClipEditor editor)
    {
        _editor = editor;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!_editor.HasSealedAnchors) return;
        if (!_editor.SealedLineNumbers.Contains(line.LineNumber)) return;

        ChangeLinePart(line.Offset, line.EndOffset, element =>
        {
            element.TextRunProperties.SetForegroundBrush(ScriptEditorTheme.SealedTextBrush);
        });
    }
}
