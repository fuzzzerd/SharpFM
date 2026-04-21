using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting;

public static class ScriptValidator
{
    public static List<ScriptDiagnostic> Validate(string displayText)
    {
        if (string.IsNullOrWhiteSpace(displayText))
            return new List<ScriptDiagnostic>();

        var diagnostics = new List<ScriptDiagnostic>();
        var textLines = displayText.Split('\n');
        var blockStack = new Stack<(string Name, int Line)>();

        // Build a continuation-line lookup: text lines INSIDE a multi-line
        // statement (lines after the step's first line, up to the closing
        // bracket) must NOT be validated as separate steps. They're part
        // of the calc above.
        var ranges = MultiLineStatementRanges.Compute(displayText);
        var continuationLineZeroIndexed = new HashSet<int>();
        foreach (var (start, end) in ranges)
        {
            for (int ln = start; ln <= end - 1; ln++)
                continuationLineZeroIndexed.Add(ln);
        }

        int stepIndex = 0;
        for (int lineNum = 0; lineNum < textLines.Length; lineNum++)
        {
            var rawLine = textLines[lineNum].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            if (continuationLineZeroIndexed.Contains(lineNum))
                continue;

            var trimmed = rawLine.TrimStart();
            var indent = rawLine.Length - trimmed.Length;

            var forLookup = trimmed;
            if (forLookup.StartsWith("//"))
                forLookup = forLookup.Substring(2).TrimStart();

            if (forLookup.StartsWith("#"))
            {
                stepIndex++;
                continue;
            }

            var bracketPos = BracketMatcher.FindTopLevelOpenBracket(forLookup);
            var stepName = bracketPos >= 0
                ? forLookup.Substring(0, bracketPos).Trim()
                : forLookup.Trim();

            if (!StepRegistry.ByName.TryGetValue(stepName, out var metadata))
            {
                var nameStart = rawLine.IndexOf(stepName, StringComparison.Ordinal);
                if (nameStart < 0) nameStart = indent;
                diagnostics.Add(new ScriptDiagnostic(
                    lineNum, nameStart, nameStart + stepName.Length,
                    $"Unknown script step '{stepName}' — preserved verbatim as a RawStep. "
                    + "Edit the underlying XML via the XML editor; display-text edits here won't round-trip.",
                    DiagnosticSeverity.Warning));
                stepIndex++;
                continue;
            }

            // Block pair validation
            if (metadata.BlockPair != null)
            {
                switch (metadata.BlockPair.Role)
                {
                    case BlockPairRole.Open:
                        blockStack.Push((metadata.Name, lineNum));
                        break;
                    case BlockPairRole.Middle:
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(
                                lineNum, indent, indent + metadata.Name.Length,
                                $"'{metadata.Name}' without matching opening step",
                                DiagnosticSeverity.Error));
                        break;
                    case BlockPairRole.Close:
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(
                                lineNum, indent, indent + metadata.Name.Length,
                                $"'{metadata.Name}' without matching opening step",
                                DiagnosticSeverity.Error));
                        else
                            blockStack.Pop();
                        break;
                }
            }

            // Validate param values
            if (bracketPos >= 0)
            {
                var parsed = ScriptLineParser.ParseRaw(rawLine);
                var usedParams = new bool[metadata.Params.Count];

                foreach (var hrParam in parsed.Params)
                {
                    var paramTrimmed = hrParam.Trim();
                    bool matchedLabel = false;

                    // First: try labeled match
                    for (int pi = 0; pi < metadata.Params.Count; pi++)
                    {
                        var catalogParam = metadata.Params[pi];
                        var label = catalogParam.HrLabel;
                        if (label == null) continue;
                        if (!paramTrimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var value = paramTrimmed.Substring(label.Length + 1).TrimStart();
                        ValidateParamValue(value, label, catalogParam, rawLine, lineNum, diagnostics);
                        usedParams[pi] = true;
                        matchedLabel = true;
                        break;
                    }

                    // Second: positional match — consume the next available
                    // param in order. Only validate the value when that
                    // param has restricted values (enum/boolean). Non-enum
                    // params (field, calc, text) accept anything, so we
                    // must NOT keep searching past them looking for an
                    // enum — that produced false-positive warnings on
                    // field references like "Assets::Selected File".
                    if (!matchedLabel && !LooksLikeCalculation(paramTrimmed))
                    {
                        for (int pi = 0; pi < metadata.Params.Count; pi++)
                        {
                            if (usedParams[pi]) continue;
                            var catalogParam = metadata.Params[pi];
                            var validValues = StepRegistry.GetValidValues(catalogParam);
                            if (validValues.Count > 0
                                && !validValues.Contains(paramTrimmed, StringComparer.OrdinalIgnoreCase))
                            {
                                var paramLabel = catalogParam.HrLabel ?? catalogParam.XmlElement;
                                ValidateParamValue(paramTrimmed, paramLabel, catalogParam, rawLine, lineNum, diagnostics);
                            }
                            usedParams[pi] = true;
                            break;
                        }
                    }
                }
            }

            stepIndex++;
        }

        // Report unclosed blocks
        while (blockStack.Count > 0)
        {
            var unclosed = blockStack.Pop();
            diagnostics.Add(new ScriptDiagnostic(
                unclosed.Line, 0, 0,
                $"'{unclosed.Name}' has no matching closing step",
                DiagnosticSeverity.Error));
        }

        return diagnostics;
    }

    private static void ValidateParamValue(
        string value, string label, ParamMetadata catalogParam,
        string rawLine, int lineNum, List<ScriptDiagnostic> diagnostics)
    {
        var validValues = StepRegistry.GetValidValues(catalogParam);
        if (validValues.Count > 0 && !validValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            var bracketStart = rawLine.IndexOf('[');
            var valuePos = bracketStart >= 0
                ? rawLine.IndexOf(value, bracketStart, StringComparison.Ordinal)
                : rawLine.IndexOf(value, StringComparison.Ordinal);
            if (valuePos < 0) valuePos = 0;
            diagnostics.Add(new ScriptDiagnostic(
                lineNum, valuePos, valuePos + value.Length,
                $"Invalid value '{value}' for {label}. Expected: {string.Join(", ", validValues)}",
                DiagnosticSeverity.Warning));
        }
    }

    private static bool LooksLikeCalculation(string value)
    {
        if (value.Length > 0 && char.IsDigit(value[0])) return true;
        return value.Contains('$') || value.Contains('(') || value.Contains('"')
            || value.Contains('>') || value.Contains('<') || value.Contains('=')
            || value.Contains('&') || value.Contains('+') || value.Contains('-')
            || value.Contains('*') || value.Contains('/');
    }
}
