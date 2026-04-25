using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Applies italic font style to sealed-step lines so they read as
/// "annotation / read-only summary" rather than as first-class code.
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepItalicColorizer : DocumentColorizingTransformer
{
    // Italic Typeface struct keyed by FontFamily — reused across every
    // SetTypeface call so we don't allocate a fresh struct per text run
    // per visible line per layout. Avalonia's font cache eventually
    // hits, but each fresh Typeface instance still drives a lookup.
    private static readonly ConcurrentDictionary<FontFamily, Typeface> ItalicCache = new();

    private readonly ScriptClipEditor _editor;

    public SealedStepItalicColorizer(ScriptClipEditor editor)
    {
        _editor = editor;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        // Fast exit: skip the SealedAnchors enumerator allocation entirely
        // when there are no sealed anchors. ColorizeLine fires per visible
        // line per layout — a hot path even when the body would no-op.
        if (!_editor.HasSealedAnchors) return;

        var doc = CurrentContext.Document;
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            if (anchor.Offset < 0 || anchor.Offset > doc.TextLength) continue;
            var anchorLine = doc.GetLineByOffset(anchor.Offset);
            if (anchorLine.LineNumber != line.LineNumber) continue;

            ChangeLinePart(line.Offset, line.EndOffset, element =>
            {
                var current = element.TextRunProperties.Typeface;
                // Skip the SetTypeface call entirely when the run is
                // already italic — Avalonia treats every SetTypeface
                // as a state change that drives ShapeTextRuns and
                // glyph-typeface resolution.
                if (current.Style == FontStyle.Italic) return;
                element.TextRunProperties.SetTypeface(GetItalic(current.FontFamily));
            });
            return;
        }
    }

    private static Typeface GetItalic(FontFamily family) =>
        ItalicCache.GetOrAdd(family, static f => new Typeface(f, FontStyle.Italic));
}
