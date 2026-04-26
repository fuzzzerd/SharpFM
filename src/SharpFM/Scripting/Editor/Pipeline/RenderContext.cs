using System;
using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SharpFM.Editors;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor.Pipeline;

/// <summary>
/// Shared state surface for every <see cref="IRenderLayer"/> in the
/// script editor's render pipeline. Layers read everything they need
/// through this object instead of subscribing to the editor's events
/// individually — the pipeline owns the subscriptions and dispatches
/// once. Cached data (statement ranges, diagnostics) lives here so the
/// layers all share one Compute pass.
/// </summary>
internal sealed class RenderContext
{
    public RenderContext(TextArea textArea)
    {
        TextArea = textArea;
    }

    public TextArea TextArea { get; }
    public TextView TextView => TextArea.TextView;
    public TextDocument? Document => TextArea.Document;
    public Caret Caret => TextArea.Caret;
    public int CaretLine => Caret.Line;
    public int CaretOffset => Caret.Offset;

    public IReadOnlyList<(int StartLine, int EndLine)> StatementRanges =>
        Document is { } d ? CachedMultiLineRanges.Compute(d) : Array.Empty<(int, int)>();

    /// <summary>
    /// Optional sealed-step source. Set by the pipeline when a
    /// <see cref="ScriptClipEditor"/> is attached so layers can read
    /// the cached sealed-line snapshot through the context rather than
    /// holding their own reference.
    /// </summary>
    public ScriptClipEditor? ClipEditor { get; private set; }

    public void SetClipEditor(ScriptClipEditor? clipEditor) => ClipEditor = clipEditor;

    private static readonly HashSet<int> EmptyLineSet = new();
    private static readonly Dictionary<int, int> EmptyEndOffsets = new();

    /// <summary>Sealed line numbers (1-based), O(1) lookup.</summary>
    public IReadOnlySet<int> SealedLineNumbers =>
        ClipEditor?.SealedLineNumbers ?? EmptyLineSet;

    /// <summary>Sealed-line end offsets keyed by line number.</summary>
    public IReadOnlyDictionary<int, int> SealedLineEndOffsets =>
        ClipEditor?.SealedLineEndOffsets ?? EmptyEndOffsets;

    public List<ScriptDiagnostic> Diagnostics { get; private set; } = new();

    /// <summary>
    /// Replace the diagnostics list. Returns true if the new list
    /// differs from the previous (count or any element field), so the
    /// pipeline can skip an InvalidateLayer when nothing visible
    /// changed.
    /// </summary>
    public bool SetDiagnostics(List<ScriptDiagnostic> next)
    {
        if (DiagnosticsEquivalent(Diagnostics, next)) return false;
        Diagnostics = next;
        return true;
    }

    private static bool DiagnosticsEquivalent(List<ScriptDiagnostic> a, List<ScriptDiagnostic> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            var x = a[i];
            var y = b[i];
            if (x.Line != y.Line || x.StartCol != y.StartCol || x.EndCol != y.EndCol
                || x.Severity != y.Severity || x.Message != y.Message)
                return false;
        }
        return true;
    }
}
