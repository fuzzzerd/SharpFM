using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Core.ScriptConverter;

public static class ScriptLineParser
{
    public static List<ParsedLine> Parse(string hrText)
    {
        if (string.IsNullOrWhiteSpace(hrText))
            return new List<ParsedLine>();

        var rawLines = hrText.Split('\n');
        var mergedLines = MergeMultilineStatements(rawLines);
        var result = new List<ParsedLine>();

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

    internal static bool HasUnbalancedBrackets(string text)
    {
        int depth = 0;
        bool inQuote = false;

        foreach (var c in text)
        {
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == '[') depth++;
            else if (!inQuote && c == ']') depth--;
        }

        return depth > 0;
    }

    public static ParsedLine ParseLine(string line)
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
            return new ParsedLine("# (comment)", new[] { commentText }, disabled, true, raw);
        }

        // Find the bracket-delimited parameters
        var bracketStart = FindTopLevelBracket(trimmed);
        if (bracketStart < 0)
        {
            // No parameters — just a step name
            return new ParsedLine(trimmed.Trim(), Array.Empty<string>(), disabled, false, raw);
        }

        var stepName = trimmed.Substring(0, bracketStart).Trim();
        var bracketEnd = FindMatchingBracket(trimmed, bracketStart);
        if (bracketEnd < 0)
            bracketEnd = trimmed.Length - 1;

        var paramText = trimmed.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
        var parameters = string.IsNullOrEmpty(paramText)
            ? Array.Empty<string>()
            : SplitParams(paramText);

        return new ParsedLine(stepName, parameters, disabled, false, raw);
    }

    internal static int FindTopLevelBracket(string text)
    {
        bool inQuote = false;
        int parenDepth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == '(') parenDepth++;
            else if (!inQuote && c == ')') parenDepth--;
            else if (!inQuote && parenDepth == 0 && c == '[')
                return i;
        }

        return -1;
    }

    internal static int FindMatchingBracket(string text, int openPos)
    {
        int depth = 1;
        bool inQuote = false;

        for (int i = openPos + 1; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == '[') depth++;
            else if (!inQuote && c == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    internal static string[] SplitParams(string paramText)
    {
        var results = new List<string>();
        int start = 0;
        int parenDepth = 0;
        int bracketDepth = 0;
        bool inQuote = false;

        for (int i = 0; i < paramText.Length; i++)
        {
            var c = paramText[i];

            if (c == '"') inQuote = !inQuote;
            else if (!inQuote)
            {
                switch (c)
                {
                    case '(': parenDepth++; break;
                    case ')': parenDepth--; break;
                    case '[': bracketDepth++; break;
                    case ']': bracketDepth--; break;
                    case ';' when parenDepth == 0 && bracketDepth == 0:
                        results.Add(paramText.Substring(start, i - start).Trim());
                        start = i + 1;
                        break;
                }
            }
        }

        // Add final segment
        var last = paramText.Substring(start).Trim();
        if (last.Length > 0)
            results.Add(last);

        return results.ToArray();
    }
}
