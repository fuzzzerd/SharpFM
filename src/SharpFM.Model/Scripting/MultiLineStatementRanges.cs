using System.Collections.Generic;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Computes the document-line ranges that each logical script step occupies.
/// A step normally lives on one line, but multi-line calculations (with
/// embedded newlines aligned to the bracket column by <see cref="FmScript.ToDisplayLines"/>)
/// produce a range whose <c>EndLine</c> exceeds <c>StartLine</c>.
/// <para>
/// Pure data — depends only on bracket balance per line via
/// <see cref="BracketMatcher"/>. Lives in the model layer so both the
/// editor renderer and the step-index margin can consume it without
/// pulling in UI dependencies.
/// </para>
/// </summary>
public static class MultiLineStatementRanges
{
    /// <summary>
    /// Compute one (start, end) range per logical line of the document.
    /// Multi-line steps (calc with newlines) span multiple physical lines.
    /// Blank lines produce single-line ranges; callers that care about
    /// "real steps only" should filter via <see cref="BuildStepIndex"/>.
    /// Line numbers are 1-indexed (AvaloniaEdit convention).
    /// </summary>
    public static List<(int StartLine, int EndLine)> Compute(string text)
    {
        var ranges = new List<(int, int)>();
        var lines = text.Split('\n');
        int currentStart = -1;
        int depth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineNum = i + 1;

            if (currentStart < 0)
            {
                if (BracketMatcher.HasUnbalancedBrackets(line))
                {
                    currentStart = lineNum;
                    depth = BracketMatcher.CountBracketDepth(line);
                }
                else
                {
                    ranges.Add((lineNum, lineNum));
                }
            }
            else
            {
                depth += BracketMatcher.CountBracketDepth(line);
                if (depth <= 0)
                {
                    ranges.Add((currentStart, lineNum));
                    currentStart = -1;
                    depth = 0;
                }
            }
        }

        if (currentStart >= 0)
            ranges.Add((currentStart, lines.Length));

        return ranges;
    }

    /// <summary>
    /// Build a lookup mapping each step's first line to its 1-based step
    /// index (FileMaker-style numbering — one number per step regardless
    /// of how many physical lines its calc spans). Blank lines ARE counted
    /// as steps — they correspond to empty <c># (comment)</c> steps in
    /// FM Pro's script model. Continuation lines of a multi-line step
    /// are absent from the map (no number rendered for them).
    /// </summary>
    public static IReadOnlyDictionary<int, int> BuildStepIndex(string text)
    {
        var ranges = Compute(text);
        var lookup = new Dictionary<int, int>(capacity: ranges.Count);
        int stepIndex = 0;

        foreach (var (start, _) in ranges)
        {
            stepIndex++;
            lookup[start] = stepIndex;
        }

        return lookup;
    }

    /// <summary>
    /// Find the column of the character immediately after the opening
    /// <c>[ </c> on a step's first line. Used by both the continuation
    /// rail renderer and the auto-indent strategy. Returns -1 if the
    /// line has no <c>[</c>.
    /// </summary>
    public static int FindContinuationColumn(string firstLine)
    {
        var bracketIdx = firstLine.IndexOf('[');
        return bracketIdx < 0 ? -1 : bracketIdx + 2;
    }

    /// <summary>
    /// Return the (start, end) range whose lines contain the given line
    /// number, or null if no range covers it. Used by the statement
    /// highlight renderer to find which step the caret is currently in.
    /// Line numbers are 1-indexed.
    /// </summary>
    public static (int StartLine, int EndLine)? FindRangeContainingLine(
        IReadOnlyList<(int StartLine, int EndLine)> ranges, int lineNumber)
    {
        foreach (var range in ranges)
        {
            if (lineNumber >= range.StartLine && lineNumber <= range.EndLine)
                return range;
        }
        return null;
    }
}
