using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;

namespace SharpFM.Scripting.Editor;

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
        // Work with text from start of line to cursor only
        var textToCursor = caretColumn >= 0 && caretColumn <= lineText.Length
            ? lineText.Substring(0, caretColumn)
            : lineText;

        var trimmed = textToCursor.TrimStart();

        // Comments don't get completions
        if (trimmed.StartsWith("#"))
            return (CompletionContext.None, Array.Empty<ICompletionData>());

        // Strip disabled prefix for lookup
        var forLookup = trimmed;
        if (forLookup.StartsWith("//"))
            forLookup = forLookup.Substring(2).TrimStart();

        // Try to find a recognized step name at the start of the line
        var (stepName, definition) = FindStepName(forLookup);

        if (definition == null)
        {
            // If we're already inside brackets of an unrecognized step, there's
            // nothing useful to suggest — this isn't a step-name context anymore.
            if (forLookup.Contains('['))
                return (CompletionContext.None, Array.Empty<ICompletionData>());

            // No recognized step yet → suggest step names
            var items = GetStepNameCompletions(forLookup);
            return (CompletionContext.StepName, items);
        }

        // Step is recognized → suggest params
        // Find the last semicolon to determine which param segment we're in
        var afterStepName = forLookup.Substring(stepName.Length);
        var lastSemicolon = afterStepName.LastIndexOf(';');
        var currentSegment = lastSemicolon >= 0
            ? afterStepName.Substring(lastSemicolon + 1).TrimStart()
            : afterStepName.TrimStart(' ', '[').TrimStart();

        // If current segment has a label, suggest values for that label
        var colonPos = currentSegment.IndexOf(':');
        if (colonPos >= 0)
        {
            var label = currentSegment.Substring(0, colonPos).Trim();
            var matchingParam = definition.Params
                .FirstOrDefault(p => (p.HrLabel ?? p.WrapperElement)?
                    .Equals(label, StringComparison.OrdinalIgnoreCase) == true);

            if (matchingParam != null)
            {
                var items = GetParamValueCompletions(matchingParam);
                if (items.Count > 0)
                    return (CompletionContext.ParamValue, items);
            }
        }

        // Suggest param labels (unused ones) and positional values
        var labelItems = GetParamLabelCompletions(definition, afterStepName);
        var valueItems = GetPositionalValueCompletions(definition, afterStepName);

        if (valueItems.Count > 0 && labelItems.Count > 0)
        {
            foreach (var item in labelItems)
                valueItems.Add(item);
            return (CompletionContext.ParamValue, valueItems);
        }

        if (valueItems.Count > 0)
            return (CompletionContext.ParamValue, valueItems);

        if (labelItems.Count > 0)
            return (CompletionContext.ParamLabel, labelItems);

        return (CompletionContext.None, Array.Empty<ICompletionData>());
    }

    private static (string Name, StepDefinition? Definition) FindStepName(string text)
    {
        // Try progressively longer prefixes to find the longest matching step name
        // Steps can have spaces in names (e.g., "Go to Record/Request/Page")
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bestMatch = (Name: "", Definition: (StepDefinition?)null);

        var candidate = "";
        foreach (var word in words)
        {
            if (candidate.Length > 0) candidate += " ";
            candidate += word;

            // Stop at bracket — everything after is params
            if (word.Contains('[')) break;

            if (StepCatalogLoader.ByName.TryGetValue(candidate, out var def))
                bestMatch = (candidate, def);
        }

        return bestMatch;
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

                // Use the Monaco snippet for full step template with placeholders
                var snippet = s.MonacoSnippet;
                // For self-closing steps without a snippet, just use the name
                if (snippet == null && s.SelfClosing)
                    snippet = s.Name;

                return (ICompletionData)new FmScriptCompletionData(s.Name, desc, snippet: snippet);
            })
            .ToList();
    }

    private static IList<ICompletionData> GetParamLabelCompletions(
        StepDefinition definition, string existingParams)
    {
        var items = new List<ICompletionData>();

        foreach (var param in definition.Params)
        {
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

        // Strip the opening bracket so it isn't counted as a positional segment
        var paramsText = existingParams.TrimStart();
        if (paramsText.StartsWith('['))
            paramsText = paramsText.Substring(1);

        var segments = paramsText.Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        int positionalIndex = 0;
        foreach (var seg in segments)
        {
            bool hasLabel = definition.Params.Any(p =>
                p.HrLabel != null && seg.StartsWith(p.HrLabel + ":", StringComparison.OrdinalIgnoreCase));
            if (!hasLabel)
                positionalIndex++;
        }

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
