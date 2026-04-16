using System.Collections.Generic;

namespace SharpFM.Editors.SealedSteps;

/// <summary>
/// Predicate: does the given selection range touch any sealed-step
/// range? Used to block copy/cut operations that would pull text out
/// of a sealed line (the display form is a lossy summary; cutting it
/// to the clipboard would let the user paste lossy content back in).
/// </summary>
public static class SealedSelectionCheck
{
    /// <summary>
    /// True iff <paramref name="selectionStart"/>..<paramref name="selectionEnd"/>
    /// overlaps any (rangeStart..rangeEnd) entry. Empty selections
    /// (start == end) always return false — a zero-length caret is not
    /// a destructive operation even when parked inside a sealed range.
    /// </summary>
    public static bool SpansSealed(
        int selectionStart,
        int selectionEnd,
        IEnumerable<(int Start, int End)> sealedRanges)
    {
        if (selectionStart == selectionEnd) return false;

        var selLo = System.Math.Min(selectionStart, selectionEnd);
        var selHi = System.Math.Max(selectionStart, selectionEnd);

        foreach (var (rStart, rEnd) in sealedRanges)
        {
            // Half-open overlap test: ranges [a,b) and [c,d) overlap iff a < d && c < b.
            // Our ranges are inclusive (a..b, c..d), so overlap iff a <= d && c <= b.
            if (selLo <= rEnd && rStart <= selHi)
                return true;
        }

        return false;
    }
}
