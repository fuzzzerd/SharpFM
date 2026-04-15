using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Model.Scripting;

public static class ScriptLineParser
{
    public static List<ParsedStep> Parse(string hrText)
    {
        if (string.IsNullOrWhiteSpace(hrText))
            return new List<ParsedStep>();

        var rawLines = hrText.Split('\n');
        var mergedLines = MergeMultilineStatements(rawLines);
        var result = new List<ParsedStep>();

        foreach (var raw in mergedLines)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            result.Add(ParseLine(raw));
        }

        return result;
    }

    /// <summary>
    /// Merge consecutive display-text lines that belong to a single logical
    /// step (continuation lines from a multi-line calculation). Render-side
    /// counterpart in <see cref="FmScript.ToDisplayLines"/> aligns continuation
    /// lines to the column just after the step's opening <c>[</c>; this method
    /// strips up to that many leading spaces from each continuation line so
    /// the user's authored calc indent (anything beyond the bracket column)
    /// survives the round-trip byte-for-byte.
    /// </summary>
    public static List<string> MergeMultilineStatements(string[] lines)
    {
        var result = new List<string>();
        System.Text.StringBuilder? accumulator = null;
        int continuationStrip = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (accumulator == null)
            {
                if (HasUnbalancedBrackets(line))
                {
                    accumulator = new System.Text.StringBuilder(line);
                    continuationStrip = ComputeContinuationStrip(line);
                }
                else
                {
                    result.Add(line);
                }
            }
            else
            {
                var stripped = StripLeadingSpaces(line, continuationStrip);
                accumulator.Append('\n').Append(stripped);
                var merged = accumulator.ToString();

                if (!HasUnbalancedBrackets(merged))
                {
                    result.Add(merged);
                    accumulator = null;
                    continuationStrip = 0;
                }
            }
        }

        if (accumulator != null)
            result.Add(accumulator.ToString());

        return result;
    }

    /// <summary>
    /// Continuation indent column for a step's first display line: the
    /// column immediately after the opening <c>[</c> + following space.
    /// Returns 0 if no <c>[</c> is present (defensive — the only path that
    /// reaches this method already requires unbalanced brackets).
    /// </summary>
    private static int ComputeContinuationStrip(string firstLine)
    {
        var bracketIdx = firstLine.IndexOf('[');
        return bracketIdx >= 0 ? bracketIdx + 2 : 0;
    }

    /// <summary>
    /// Strip up to <paramref name="maxSpaces"/> leading spaces. If the line
    /// has fewer leading spaces than the target (user deleted some), strip
    /// only what's there. Anything beyond the target is part of the user's
    /// calc content and stays.
    /// </summary>
    private static string StripLeadingSpaces(string line, int maxSpaces)
    {
        int n = 0;
        while (n < line.Length && n < maxSpaces && line[n] == ' ')
            n++;
        return n == 0 ? line : line.Substring(n);
    }

    public static bool HasUnbalancedBrackets(string text) =>
        BracketMatcher.HasUnbalancedBrackets(text);

    // Used by ScriptStep.FromDisplayLine — returns the same data as ParseLine
    // but avoids coupling ScriptStep to ParsedStep
    public static ParsedStep ParseRaw(string line) => ParseLine(line);

    public static ParsedStep ParseLine(string line)
    {
        var raw = line;
        var trimmed = line.TrimStart();
        bool disabled = false;

        // Check for disabled prefix
        if (trimmed.StartsWith("//"))
        {
            disabled = true;
            trimmed = trimmed.Substring(2).TrimStart();
        }

        // Check for comment
        if (trimmed.StartsWith("#"))
        {
            var commentText = trimmed.Length > 1 ? trimmed.Substring(1).TrimStart() : "";
            return new ParsedStep("# (comment)", new[] { commentText }, disabled, true, raw);
        }

        // Find the bracket-delimited parameters
        var bracketStart = BracketMatcher.FindTopLevelOpenBracket(trimmed);
        if (bracketStart < 0)
        {
            // No parameters — just a step name
            return new ParsedStep(trimmed.Trim(), Array.Empty<string>(), disabled, false, raw);
        }

        var stepName = trimmed.Substring(0, bracketStart).Trim();
        var bracketEnd = BracketMatcher.FindMatchingClose(trimmed, bracketStart);
        if (bracketEnd < 0)
            bracketEnd = trimmed.Length - 1;

        var paramLength = bracketEnd - bracketStart - 1;
        if (paramLength <= 0)
            return new ParsedStep(stepName, Array.Empty<string>(), disabled, false, raw);

        var paramText = trimmed.Substring(bracketStart + 1, paramLength).Trim();
        var parameters = string.IsNullOrEmpty(paramText)
            ? Array.Empty<string>()
            : BracketMatcher.SplitParams(paramText);

        return new ParsedStep(stepName, parameters, disabled, false, raw);
    }
}
