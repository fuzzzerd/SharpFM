using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Per-document cache for <see cref="MultiLineStatementRanges.Compute"/>
/// results. The compute is pure on document text — every renderer +
/// caret handler calling it independently used to do the same full-doc
/// scan multiple times per edit. With this wrapper the work runs once
/// per document version and all callers reuse the result.
///
/// <para>Cache lifetime is tied to the <see cref="TextDocument"/> via
/// <see cref="ConditionalWeakTable{TKey,TValue}"/>, so closing a
/// document drops its cache entry automatically.</para>
/// </summary>
[ExcludeFromCodeCoverage]
public static class CachedMultiLineRanges
{
    private sealed class CacheEntry
    {
        public ITextSourceVersion? Version;
        public List<(int StartLine, int EndLine)>? Ranges;
        public IReadOnlyDictionary<int, int>? StepIndex;
    }

    private static readonly ConditionalWeakTable<TextDocument, CacheEntry> Cache = new();

    /// <summary>
    /// Statement ranges for the document. Cached by version — repeated
    /// calls within the same edit cycle return the previously computed
    /// list with no recomputation.
    /// </summary>
    public static List<(int StartLine, int EndLine)> Compute(TextDocument document)
    {
        var entry = Cache.GetValue(document, static _ => new CacheEntry());
        var version = document.Version;

        if (entry.Ranges != null && SameVersion(entry.Version, version))
            return entry.Ranges;

        var ranges = MultiLineStatementRanges.Compute(document.Text);
        entry.Version = version;
        entry.Ranges = ranges;
        // Step-index recomputes lazily on first call after a version change.
        entry.StepIndex = null;
        return ranges;
    }

    /// <summary>
    /// Step-index lookup (first-line → 1-based step number) for the
    /// document. Cached jointly with the statement ranges.
    /// </summary>
    public static IReadOnlyDictionary<int, int> GetStepIndex(TextDocument document)
    {
        var entry = Cache.GetValue(document, static _ => new CacheEntry());
        var version = document.Version;

        if (entry.StepIndex != null && SameVersion(entry.Version, version))
            return entry.StepIndex;

        // Reuse the cached ranges if they match this version, otherwise
        // Compute will refresh them.
        var ranges = (entry.Ranges != null && SameVersion(entry.Version, version))
            ? entry.Ranges
            : Compute(document);

        var lookup = new Dictionary<int, int>(capacity: ranges.Count);
        int stepIndex = 0;
        foreach (var (start, _) in ranges)
        {
            stepIndex++;
            lookup[start] = stepIndex;
        }

        entry.StepIndex = lookup;
        return lookup;
    }

    private static bool SameVersion(ITextSourceVersion? a, ITextSourceVersion? b)
    {
        if (a is null || b is null) return false;
        if (!a.BelongsToSameDocumentAs(b)) return false;
        return a.CompareAge(b) == 0;
    }
}
