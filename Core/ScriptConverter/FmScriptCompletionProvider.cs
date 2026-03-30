using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;

namespace SharpFM.Core.ScriptConverter;

public enum CompletionContext
{
    StepName,
    ParamLabel,
    ParamValue,
    None
}

public static class FmScriptCompletionProvider
{
    public static (CompletionContext Context, IList<ICompletionData> Items) GetCompletions(
        string lineText, int caretColumn)
    {
        var trimmed = lineText.TrimStart();

        // Empty or start of line → suggest step names
        if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.Contains('['))
        {
            var items = GetStepNameCompletions(trimmed);
            return (CompletionContext.StepName, items);
        }

        // Inside brackets → determine if we're after a label or need a label
        var bracketPos = lineText.IndexOf('[');
        if (bracketPos >= 0 && caretColumn > bracketPos)
        {
            var insideBrackets = lineText.Substring(bracketPos + 1);
            var stepName = lineText.Substring(0, bracketPos).Trim();

            // Check for disabled prefix
            if (stepName.StartsWith("//"))
                stepName = stepName.Substring(2).TrimStart();

            if (!StepCatalogLoader.ByName.TryGetValue(stepName, out var definition))
                return (CompletionContext.None, Array.Empty<ICompletionData>());

            // Find what the user is currently typing after the last semicolon
            var lastSemicolon = insideBrackets.LastIndexOf(';');
            var currentSegment = lastSemicolon >= 0
                ? insideBrackets.Substring(lastSemicolon + 1).TrimStart()
                : insideBrackets.TrimStart();

            // Check if we're after a label (e.g., "With dialog: ")
            var colonPos = currentSegment.IndexOf(':');
            if (colonPos >= 0)
            {
                var label = currentSegment.Substring(0, colonPos).Trim();
                var matchingParam = definition.Params
                    .FirstOrDefault(p => p.HrLabel != null &&
                        p.HrLabel.Equals(label, StringComparison.OrdinalIgnoreCase));

                if (matchingParam != null)
                {
                    var items = GetParamValueCompletions(matchingParam);
                    return (CompletionContext.ParamValue, items);
                }
            }

            // Suggest param labels + valid values for unlabeled positional params
            var labelItems = GetParamLabelCompletions(definition, insideBrackets);
            var valueItems = GetPositionalValueCompletions(definition, insideBrackets);

            if (valueItems.Count > 0 && labelItems.Count > 0)
            {
                // Combine: values first (more immediately useful), then labels
                foreach (var item in labelItems)
                    valueItems.Add(item);
                return (CompletionContext.ParamValue, valueItems);
            }

            if (valueItems.Count > 0)
                return (CompletionContext.ParamValue, valueItems);

            return (CompletionContext.ParamLabel, labelItems);
        }

        return (CompletionContext.None, Array.Empty<ICompletionData>());
    }

    private static IList<ICompletionData> GetStepNameCompletions(string prefix)
    {
        return StepCatalogLoader.All
            .Where(s => s.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrEmpty(prefix))
            .OrderBy(s => s.Name)
            .Select(s =>
            {
                var desc = s.Category;
                if (s.BlockPair != null) desc += $" ({s.BlockPair.Role})";
                if (!string.IsNullOrEmpty(s.HrSignature)) desc += $" {s.HrSignature}";
                return (ICompletionData)new FmScriptCompletionData(s.Name, desc);
            })
            .ToList();
    }

    private static IList<ICompletionData> GetParamLabelCompletions(
        StepDefinition definition, string existingParams)
    {
        var items = new List<ICompletionData>();

        foreach (var param in definition.Params)
        {
            // Use hrLabel, or wrapperElement as synthetic label for namedCalc params
            var label = param.HrLabel
                ?? (param.Type == "namedCalc" && param.WrapperElement != null
                    ? param.WrapperElement : null);
            if (label == null) continue;

            // Skip labels already used
            if (existingParams.Contains(label + ":", StringComparison.OrdinalIgnoreCase))
                continue;

            var desc = $"{param.Type}";
            var validValues = ScriptValidator.GetValidValues(param);
            if (validValues.Count > 0)
                desc += $" ({string.Join("|", validValues)})";

            items.Add(new FmScriptCompletionData(label + ": ", desc));
        }

        return items;
    }

    private static IList<ICompletionData> GetParamValueCompletions(StepParam param)
    {
        var validValues = ScriptValidator.GetValidValues(param);
        return validValues
            .Select(v => (ICompletionData)new FmScriptCompletionData(v))
            .ToList();
    }

    private static IList<ICompletionData> GetPositionalValueCompletions(
        StepDefinition definition, string existingParams)
    {
        var items = new List<ICompletionData>();

        // Count how many unlabeled positional params have been filled
        var segments = existingParams.Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        int positionalIndex = 0;
        foreach (var seg in segments)
        {
            // If it has a label (contains ":"), it's not positional
            bool hasLabel = definition.Params.Any(p =>
                p.HrLabel != null && seg.StartsWith(p.HrLabel + ":", StringComparison.OrdinalIgnoreCase));
            if (!hasLabel)
                positionalIndex++;
        }

        // Find the next unlabeled param with valid values
        int unlabeledCount = 0;
        foreach (var param in definition.Params)
        {
            if (param.HrLabel != null) continue;

            if (unlabeledCount == positionalIndex)
            {
                var values = ScriptValidator.GetValidValues(param);
                foreach (var v in values)
                    items.Add(new FmScriptCompletionData(v, $"{param.XmlElement}"));
                break;
            }
            unlabeledCount++;
        }

        return items;
    }
}
