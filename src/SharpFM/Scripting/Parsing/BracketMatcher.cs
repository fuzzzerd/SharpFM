namespace SharpFM.Scripting.Parsing;

/// <summary>
/// Shared bracket matching utilities. All bracket/quote-aware logic
/// goes through here to avoid duplication across parser, renderers, and model.
/// </summary>
internal static class BracketMatcher
{
    /// <summary>
    /// Find the first top-level '[' that isn't inside quotes or parentheses.
    /// </summary>
    internal static int FindTopLevelOpenBracket(string text)
    {
        bool inQuote = false;
        int parenDepth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && inQuote && i + 1 < text.Length) { i++; continue; }
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == '(') parenDepth++;
            else if (!inQuote && c == ')') parenDepth--;
            else if (!inQuote && parenDepth == 0 && c == '[')
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Find the matching ']' for the '[' at openPos.
    /// </summary>
    internal static int FindMatchingClose(string text, int openPos)
    {
        int depth = 1;
        bool inQuote = false;

        for (int i = openPos + 1; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && inQuote && i + 1 < text.Length) { i++; continue; }
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

    /// <summary>
    /// Find the matching '[' for the ']' before closePos (scanning backwards).
    /// </summary>
    internal static int FindMatchingOpen(string text, int closePos)
    {
        int depth = 1;
        bool inQuote = false;

        for (int i = closePos; i >= 0; i--)
        {
            var c = text[i];
            // Note: backward escape detection is imprecise, but sufficient for display
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == ']') depth++;
            else if (!inQuote && c == '[')
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// True if the text has more '[' than ']' (unbalanced, needs continuation lines).
    /// </summary>
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

    /// <summary>
    /// Count the net bracket depth change of a line (positive = more opens than closes).
    /// </summary>
    internal static int CountBracketDepth(string line)
    {
        int depth = 0;
        bool inQuote = false;

        foreach (var c in line)
        {
            if (c == '"') inQuote = !inQuote;
            else if (!inQuote && c == '[') depth++;
            else if (!inQuote && c == ']') depth--;
        }

        return depth;
    }

    /// <summary>
    /// Split parameters by top-level semicolons (respecting quotes, parens, brackets).
    /// </summary>
    internal static string[] SplitParams(string paramText)
    {
        var results = new System.Collections.Generic.List<string>();
        int start = 0;
        int parenDepth = 0;
        int bracketDepth = 0;
        bool inQuote = false;

        for (int i = 0; i < paramText.Length; i++)
        {
            var c = paramText[i];
            if (c == '\\' && inQuote && i + 1 < paramText.Length) { i++; continue; }
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

        var last = paramText.Substring(start).Trim();
        if (last.Length > 0)
            results.Add(last);

        return results.ToArray();
    }
}
