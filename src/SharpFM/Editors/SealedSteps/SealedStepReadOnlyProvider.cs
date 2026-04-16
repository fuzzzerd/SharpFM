using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// AvaloniaEdit read-only-section provider that marks every sealed-step
/// line as read-only. Lookups are driven off the anchor cache held by
/// <see cref="ScriptClipEditor"/>: an anchor's line is read-only until
/// it's deleted (user removes the whole line) or the step gets promoted
/// to fully editable via the allow-list.
/// </summary>
[ExcludeFromCodeCoverage]
public class SealedStepReadOnlyProvider : IReadOnlySectionProvider
{
    private readonly TextDocument _document;
    private readonly ScriptClipEditor _editor;

    public SealedStepReadOnlyProvider(TextDocument document, ScriptClipEditor editor)
    {
        _document = document;
        _editor = editor;
    }

    public bool CanInsert(int offset) =>
        !IsOffsetInsideSealedLine(offset);

    public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
    {
        var sealedLines = CollectSealedLineRanges().ToList();
        if (sealedLines.Count == 0)
        {
            yield return segment;
            yield break;
        }

        // Emit the subsegments of `segment` that don't intersect any sealed line.
        int cursor = segment.Offset;
        int end = segment.EndOffset;

        foreach (var (rStart, rEnd) in sealedLines.OrderBy(r => r.Start))
        {
            if (rEnd < cursor) continue;
            if (rStart >= end) break;

            if (rStart > cursor)
                yield return new TextSegment { StartOffset = cursor, EndOffset = rStart };

            cursor = System.Math.Max(cursor, rEnd + 1);
        }

        if (cursor < end)
            yield return new TextSegment { StartOffset = cursor, EndOffset = end };
    }

    private bool IsOffsetInsideSealedLine(int offset)
    {
        foreach (var (start, endOffset) in CollectSealedLineRanges())
        {
            if (offset > start && offset <= endOffset) return true;
        }
        return false;
    }

    private IEnumerable<(int Start, int End)> CollectSealedLineRanges()
    {
        foreach (var anchor in _editor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            var line = _document.GetLineByOffset(anchor.Offset);
            yield return (line.Offset, line.EndOffset);
        }
    }
}
