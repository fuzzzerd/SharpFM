using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Core.ScriptConverter;

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

    internal static List<string> MergeMultilineStatements(string[] lines)
    {
        var result = new List<string>();
        string? accumulator = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (accumulator == null)
            {
                if (HasUnbalancedBrackets(line))
                {
                    accumulator = line;
                }
                else
                {
                    result.Add(line);
                }
            }
            else
            {
                // Continue merging — preserve the newline for readability
                accumulator += "\n" + line;

                if (!HasUnbalancedBrackets(accumulator))
                {
                    result.Add(accumulator);
                    accumulator = null;
                }
            }
        }

        // If still accumulating at end, emit what we have
        if (accumulator != null)
            result.Add(accumulator);

        return result;
    }

    internal static bool HasUnbalancedBrackets(string text) =>
        BracketMatcher.HasUnbalancedBrackets(text);

    // Used by ScriptStep.FromDisplayLine — returns the same data as ParseLine
    // but avoids coupling ScriptStep to ParsedStep
    internal static ParsedStep ParseRaw(string line) => ParseLine(line);

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

        var paramText = trimmed.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
        var parameters = string.IsNullOrEmpty(paramText)
            ? Array.Empty<string>()
            : BracketMatcher.SplitParams(paramText);

        return new ParsedStep(stepName, parameters, disabled, false, raw);
    }
}
