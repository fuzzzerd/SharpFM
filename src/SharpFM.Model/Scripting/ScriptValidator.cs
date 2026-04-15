using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
            // start/end are 1-indexed in ranges. Continuation lines are
            // (start+1)..end inclusive — converted to 0-indexed = start..(end-1).
            for (int ln = start; ln <= end - 1; ln++)
                continuationLineZeroIndexed.Add(ln);
        }

        // Walk actual text lines for correct positions
        int stepIndex = 0;
        for (int lineNum = 0; lineNum < textLines.Length; lineNum++)
        {
            var rawLine = textLines[lineNum].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            // Skip continuation lines of a multi-line step — they are
            // parsed as part of the owning step's calc, not standalone steps.
            if (continuationLineZeroIndexed.Contains(lineNum))
                continue;

            var trimmed = rawLine.TrimStart();
            var indent = rawLine.Length - trimmed.Length;

            // Strip disabled prefix for step name lookup
            var forLookup = trimmed;
            if (forLookup.StartsWith("//"))
                forLookup = forLookup.Substring(2).TrimStart();

            // Comments are always valid
            if (forLookup.StartsWith("#"))
            {
                stepIndex++;
                continue;
            }

            // Extract step name (text before '[' or end of line)
            var bracketPos = BracketMatcher.FindTopLevelOpenBracket(forLookup);
            var stepName = bracketPos >= 0
                ? forLookup.Substring(0, bracketPos).Trim()
                : forLookup.Trim();

            // Check if step exists
            if (!StepCatalogLoader.ByName.TryGetValue(stepName, out var definition))
            {
                // Underline the step name portion of the line
                var nameStart = rawLine.IndexOf(stepName, StringComparison.Ordinal);
                if (nameStart < 0) nameStart = indent;
                diagnostics.Add(new ScriptDiagnostic(
                    lineNum, nameStart, nameStart + stepName.Length,
                    $"Unknown script step: '{stepName}'",
                    DiagnosticSeverity.Error));
                stepIndex++;
                continue;
            }

            // Block pair validation
            if (definition.BlockPair != null)
            {
                switch (definition.BlockPair.Role)
                {
                    case BlockPairRole.Open:
                        blockStack.Push((definition.Name, lineNum));
                        break;
                    case BlockPairRole.Middle:
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(
                                lineNum, indent, indent + definition.Name.Length,
                                $"'{definition.Name}' without matching opening step",
                                DiagnosticSeverity.Error));
                        break;
                    case BlockPairRole.Close:
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(
                                lineNum, indent, indent + definition.Name.Length,
                                $"'{definition.Name}' without matching opening step",
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
                var usedParams = new bool[definition.Params.Length];

                foreach (var hrParam in parsed.Params)
                {
                    var paramTrimmed = hrParam.Trim();
                    bool matchedLabel = false;

                    // First: try labeled match
                    for (int pi = 0; pi < definition.Params.Length; pi++)
                    {
                        var catalogParam = definition.Params[pi];
                        var label = catalogParam.HrLabel
                            ?? (catalogParam.Type == "namedCalc" && catalogParam.WrapperElement != null
                                ? catalogParam.WrapperElement : null);
                        if (label == null) continue;
                        if (!paramTrimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var value = paramTrimmed.Substring(label.Length + 1).TrimStart();
                        ValidateParamValue(value, label, catalogParam, rawLine, lineNum, diagnostics);
                        usedParams[pi] = true;
                        matchedLabel = true;
                        break;
                    }

                    // Second: positional match for unlabeled enum/boolean params
                    if (!matchedLabel && !LooksLikeCalculation(paramTrimmed))
                    {
                        for (int pi = 0; pi < definition.Params.Length; pi++)
                        {
                            if (usedParams[pi]) continue;
                            var catalogParam = definition.Params[pi];
                            var validValues = GetValidValues(catalogParam);
                            if (validValues.Count == 0) continue;

                            // This param has restricted values — check
                            if (!validValues.Contains(paramTrimmed, StringComparer.OrdinalIgnoreCase))
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
        string value, string label, StepParam catalogParam,
        string rawLine, int lineNum, List<ScriptDiagnostic> diagnostics)
    {
        var validValues = GetValidValues(catalogParam);
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
        // Skip validation for values that appear to be calculations, expressions, or literals
        if (value.Length > 0 && char.IsDigit(value[0])) return true; // numeric literal
        return value.Contains('$') || value.Contains('(') || value.Contains('"')
            || value.Contains('>') || value.Contains('<') || value.Contains('=')
            || value.Contains('&') || value.Contains('+') || value.Contains('-')
            || value.Contains('*') || value.Contains('/');
    }

    public static List<string> GetValidValues(StepParam param)
    {
        var valid = new List<string>();

        if (param.Type is "boolean" or "flagBoolean" or "flagElement")
        {
            if (param.HrEnumValues != null)
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            else if (param.HrValues is { Length: > 0 })
                valid.AddRange(param.HrValues);
            else
            {
                valid.Add("On");
                valid.Add("Off");
            }
        }
        else if (param.Type == "enum")
        {
            if (param.HrEnumValues != null)
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            else if (param.EnumValues != null)
            {
                foreach (var ev in param.EnumValues)
                {
                    if (ev.ValueKind == JsonValueKind.String)
                        valid.Add(ev.GetString()!);
                    else if (ev.ValueKind == JsonValueKind.Object && ev.TryGetProperty("hr", out var hr))
                    {
                        var hrStr = hr.GetString();
                        if (!string.IsNullOrEmpty(hrStr))
                            valid.Add(hrStr);
                    }
                }
            }
        }

        return valid;
    }
}
