using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SharpFM.Core.ScriptConverter;

public static class ScriptValidator
{
    public static List<ScriptDiagnostic> Validate(string hrText)
    {
        var diagnostics = new List<ScriptDiagnostic>();

        if (string.IsNullOrWhiteSpace(hrText))
            return diagnostics;

        var lines = hrText.Split('\n');
        var blockStack = new Stack<(string StepName, int Line)>();

        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var parsed = ScriptLineParser.ParseLine(raw);

            // Comments are always valid
            if (parsed.IsComment)
                continue;

            // Validate step name
            if (!StepCatalogLoader.ByName.TryGetValue(parsed.StepName, out var definition))
            {
                var col = raw.IndexOf(parsed.StepName, StringComparison.Ordinal);
                if (col < 0) col = raw.Length - raw.TrimStart().Length;
                diagnostics.Add(new ScriptDiagnostic(
                    i, col, col + parsed.StepName.Length,
                    $"Unknown script step: '{parsed.StepName}'",
                    DiagnosticSeverity.Error));
                continue;
            }

            // Track block pairs
            ValidateBlockPairs(definition, i, blockStack, diagnostics);

            // Validate parameters
            if (parsed.Params.Length > 0)
                ValidateParams(parsed, definition, raw, i, diagnostics);
        }

        // Report unclosed blocks
        while (blockStack.Count > 0)
        {
            var unclosed = blockStack.Pop();
            diagnostics.Add(new ScriptDiagnostic(
                unclosed.Line, 0, 0,
                $"'{unclosed.StepName}' has no matching closing step",
                DiagnosticSeverity.Error));
        }

        return diagnostics;
    }

    private static void ValidateBlockPairs(
        StepDefinition definition, int line,
        Stack<(string StepName, int Line)> blockStack,
        List<ScriptDiagnostic> diagnostics)
    {
        if (definition.BlockPair == null) return;

        switch (definition.BlockPair.Role)
        {
            case "open":
                blockStack.Push((definition.Name, line));
                break;

            case "middle":
                if (blockStack.Count == 0)
                {
                    diagnostics.Add(new ScriptDiagnostic(
                        line, 0, definition.Name.Length,
                        $"'{definition.Name}' without matching opening step",
                        DiagnosticSeverity.Error));
                }
                break;

            case "close":
                if (blockStack.Count == 0)
                {
                    diagnostics.Add(new ScriptDiagnostic(
                        line, 0, definition.Name.Length,
                        $"'{definition.Name}' without matching opening step",
                        DiagnosticSeverity.Error));
                }
                else
                {
                    blockStack.Pop();
                }
                break;
        }
    }

    private static void ValidateParams(
        ParsedLine parsed, StepDefinition definition, string rawLine, int line,
        List<ScriptDiagnostic> diagnostics)
    {
        // Only validate labeled params that the user explicitly typed.
        // Positional matching is unreliable for validation because calculations
        // can be mistaken for boolean/enum values.
        foreach (var hrParam in parsed.Params)
        {
            var trimmed = hrParam.Trim();

            // Try to find a labeled match: "Label: value"
            foreach (var catalogParam in definition.Params)
            {
                var label = catalogParam.HrLabel
                    ?? (catalogParam.Type == "namedCalc" && catalogParam.WrapperElement != null
                        ? catalogParam.WrapperElement : null);
                if (label == null) continue;
                if (!trimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = trimmed.Substring(label.Length + 1).TrimStart();

                var validValues = GetValidValues(catalogParam);
                if (validValues.Count > 0 && !validValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    var valuePos = FindValuePosition(rawLine, value);
                    diagnostics.Add(new ScriptDiagnostic(
                        line, valuePos, valuePos + value.Length,
                        $"Invalid value '{value}' for {catalogParam.HrLabel}. Expected: {string.Join(", ", validValues)}",
                        DiagnosticSeverity.Warning));
                }
                break;
            }
        }
    }

    internal static List<string> GetValidValues(StepParam param)
    {
        var valid = new List<string>();

        if (param.Type is "boolean" or "flagBoolean" or "flagElement")
        {
            if (param.HrEnumValues != null)
            {
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            }
            else if (param.HrValues is { Length: > 0 })
            {
                valid.AddRange(param.HrValues);
            }
            else
            {
                valid.Add("On");
                valid.Add("Off");
            }
        }
        else if (param.Type == "enum")
        {
            if (param.HrEnumValues != null)
            {
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            }
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

    private static int FindValuePosition(string line, string value)
    {
        // Search from the bracket section onward to avoid matching step name
        var bracketPos = line.IndexOf('[');
        if (bracketPos >= 0)
        {
            var pos = line.IndexOf(value, bracketPos, StringComparison.Ordinal);
            if (pos >= 0) return pos;
        }
        // Fallback
        var fallback = line.IndexOf(value, StringComparison.Ordinal);
        return fallback >= 0 ? fallback : 0;
    }
}
