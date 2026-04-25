using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Scripting.Editor;

public enum CompletionContext
{
    StepName,
    ParamLabel,
    ParamValue,
    /// <summary>
    /// Caret is inside <c>[ ... ]</c> on a freeform value position — the
    /// calculation provider is supplying functions, control forms,
    /// constants, etc. Anchored at the identifier prefix.
    /// </summary>
    CalcExpression,
    /// <summary>
    /// Same as <see cref="CalcExpression"/> but the calc provider matched
    /// a <see cref="FmCalcCompletionProvider"/> <c>FunctionParam</c>
    /// context — i.e. enum keywords for the current arg
    /// (<c>Get(...)</c>, <c>JSONSetElement</c>'s <c>type</c>, etc.).
    /// Distinguished so the controller can pop on <c>(</c> and <c>;</c>
    /// triggers without spamming bare identifier lists.
    /// </summary>
    CalcParamValue,
    None,
}

/// <summary>
/// Completion provider for the script editor. Reads exclusively from
/// <see cref="StepRegistry"/> — the typed POCO registry populated by
/// reflection over <c>IStepFactory</c> implementers in the model
/// assembly.
/// </summary>
public static class FmScriptCompletionProvider
{
    public static (CompletionContext Context, IList<ICompletionData> Items) GetCompletions(
        string lineText, int caretColumn)
    {
        var textToCursor = caretColumn >= 0 && caretColumn <= lineText.Length
            ? lineText.Substring(0, caretColumn)
            : lineText;

        var trimmed = textToCursor.TrimStart();

        if (trimmed.StartsWith("#"))
            return (CompletionContext.None, Array.Empty<ICompletionData>());

        var forLookup = trimmed;
        if (forLookup.StartsWith("//"))
            forLookup = forLookup.Substring(2).TrimStart();

        var (stepName, metadata) = FindStepName(forLookup);

        if (metadata == null)
        {
            if (forLookup.Contains('['))
                return (CompletionContext.None, Array.Empty<ICompletionData>());

            var items = GetStepNameCompletions(forLookup);
            return (CompletionContext.StepName, items);
        }

        // Inside an unmatched `(...)` the `;` separators belong to that
        // call, not to the step's bracket-param list. Hand off to the calc
        // provider before we try to slice currentSegment on flat `;` —
        // otherwise the script logic mis-attributes the call's args to
        // step params and we never reach the labeled-value fallback.
        if (FmCalcCompletionProvider.DetectEnclosingCall(lineText, caretColumn) != null)
        {
            var deferredEarly = DeferToCalc(lineText, caretColumn);
            if (deferredEarly.Items.Count > 0) return deferredEarly;
        }

        var afterStepName = forLookup.Substring(stepName.Length);
        var lastSemicolon = afterStepName.LastIndexOf(';');
        var currentSegment = lastSemicolon >= 0
            ? afterStepName.Substring(lastSemicolon + 1).TrimStart()
            : afterStepName.TrimStart(' ', '[').TrimStart();

        var colonPos = currentSegment.IndexOf(':');
        if (colonPos >= 0)
        {
            var label = currentSegment.Substring(0, colonPos).Trim();
            var matchingParam = metadata.Params.FirstOrDefault(p =>
                p.HrLabel?.Equals(label, StringComparison.OrdinalIgnoreCase) == true);

            if (matchingParam != null)
            {
                var items = GetParamValueCompletions(matchingParam);
                if (items.Count > 0)
                    return (CompletionContext.ParamValue, items);

                // Param exists but has no enum values — the user is typing
                // a freeform calculation expression (e.g. `Value: Length(`,
                // `Value: $myVar`, `Value: Get(AccountName)`). Hand off to
                // the calc provider so functions, control forms, constants
                // and per-arg keywords all light up here too.
                var deferred = DeferToCalc(lineText, caretColumn);
                if (deferred.Items.Count > 0) return deferred;
            }
        }

        var labelItems = GetParamLabelCompletions(metadata, afterStepName);
        var valueItems = GetPositionalValueCompletions(metadata, afterStepName);

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

    private static (string Name, StepMetadata? Metadata) FindStepName(string text)
    {
        // Try progressively longer prefixes to find the longest matching
        // step name. Step names can contain spaces and punctuation (e.g.
        // "Go to Record/Request/Page") so we walk whitespace-separated
        // tokens and consult the registry at each boundary.
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bestMatch = (Name: "", Metadata: (StepMetadata?)null);

        var candidate = "";
        foreach (var word in words)
        {
            if (candidate.Length > 0) candidate += " ";
            candidate += word;

            if (word.Contains('[')) break;

            if (StepRegistry.ByName.TryGetValue(candidate, out var md))
                bestMatch = (candidate, md);
        }

        return bestMatch;
    }

    /// <summary>
    /// Every step-name completion, built once at type initialisation. The
    /// step registry is immutable after startup, so this list is reused
    /// across every script editor — per-keystroke work is just a
    /// prefix-filtered copy of references. Mirrors
    /// <see cref="FmCalcCompletionProvider.AllBuiltinIdentifierCompletions"/>.
    /// </summary>
    public static IReadOnlyList<ICompletionData> AllStepNameCompletions { get; } =
        BuildAllStepNameCompletions();

    private static IReadOnlyList<ICompletionData> BuildAllStepNameCompletions()
    {
        return StepRegistry.All
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .Select(s =>
            {
                var desc = s.Category;
                if (s.BlockPair != null) desc += $" ({s.BlockPair.Role})";
                if (!string.IsNullOrEmpty(s.HrSignature)) desc += $" {s.HrSignature}";

                return (ICompletionData)new FmScriptCompletionData(
                    s.Name, desc, snippet: SynthesizeStepSnippet(s));
            })
            .ToList();
    }

    private static IList<ICompletionData> GetStepNameCompletions(string prefix)
    {
        var items = new List<ICompletionData>();
        foreach (var item in AllStepNameCompletions)
        {
            if (string.IsNullOrEmpty(prefix) ||
                item.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(item);
            }
        }
        return items;
    }

    /// <summary>
    /// Build a Monaco-style snippet (<c>${N:placeholder}</c> tab-stops)
    /// from the step's <see cref="ParamMetadata"/> so that accepting a
    /// step-name completion inserts the full bracketed form with the
    /// first parameter pre-selected for immediate editing.
    ///
    /// <para>
    /// Without this, multi-param steps complete to the bare name and the
    /// user ends up with <c>Set Error Capture On</c> instead of
    /// <c>Set Error Capture [ On ]</c> when the param-value prompt fires
    /// next — the param-value path doesn't know to add brackets.
    /// </para>
    /// </summary>
    private static string SynthesizeStepSnippet(StepMetadata metadata)
    {
        if (metadata.Params.Count == 0)
            return metadata.Name;

        var segments = new List<string>(metadata.Params.Count);
        int index = 1;
        foreach (var param in metadata.Params)
        {
            var placeholder = param.ValidValues?.FirstOrDefault()
                ?? param.DefaultValue
                ?? param.HrLabel
                ?? param.Name
                ?? "value";
            var tabStop = $"${{{index++}:{placeholder}}}";
            segments.Add(param.HrLabel != null ? $"{param.HrLabel}: {tabStop}" : tabStop);
        }

        return $"{metadata.Name} [ {string.Join(" ; ", segments)} ]";
    }

    private static IList<ICompletionData> GetParamLabelCompletions(
        StepMetadata metadata, string existingParams)
    {
        var items = new List<ICompletionData>();

        foreach (var param in metadata.Params)
        {
            var label = param.HrLabel;
            if (label == null) continue;

            if (existingParams.Contains(label + ":", StringComparison.OrdinalIgnoreCase))
                continue;

            var desc = param.Type;
            var validValues = StepRegistry.GetValidValues(param);
            if (validValues.Count > 0)
                desc += $" ({string.Join("|", validValues)})";

            items.Add(new FmScriptCompletionData(label + ": ", desc));
        }

        return items;
    }

    private static IList<ICompletionData> GetParamValueCompletions(ParamMetadata param)
    {
        var validValues = StepRegistry.GetValidValues(param);
        return validValues
            .Select(v => (ICompletionData)new FmScriptCompletionData(v))
            .ToList();
    }

    private static IList<ICompletionData> GetPositionalValueCompletions(
        StepMetadata metadata, string existingParams)
    {
        var items = new List<ICompletionData>();

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
            bool hasLabel = metadata.Params.Any(p =>
                p.HrLabel != null && seg.StartsWith(p.HrLabel + ":", StringComparison.OrdinalIgnoreCase));
            if (!hasLabel)
                positionalIndex++;
        }

        int unlabeledCount = 0;
        foreach (var param in metadata.Params)
        {
            if (param.HrLabel != null) continue;

            if (unlabeledCount == positionalIndex)
            {
                var values = StepRegistry.GetValidValues(param);
                foreach (var v in values)
                    items.Add(new FmScriptCompletionData(v, param.XmlElement));
                break;
            }
            unlabeledCount++;
        }

        return items;
    }

    /// <summary>
    /// Hand off completion to the calc provider for the same line/caret
    /// position. Returns a script-side context so the controller can
    /// distinguish identifier-prefix completions (anchored at the prefix)
    /// from function-param keyword completions (whose triggers also
    /// include <c>(</c> and <c>;</c>).
    /// </summary>
    private static (CompletionContext Context, IList<ICompletionData> Items) DeferToCalc(
        string lineText, int caretColumn)
    {
        var (calcCtx, calcItems) = FmCalcCompletionProvider.GetCompletions(lineText, caretColumn);
        if (calcCtx == CalcCompletionContext.None || calcItems.Count == 0)
            return (CompletionContext.None, Array.Empty<ICompletionData>());

        var scriptCtx = calcCtx == CalcCompletionContext.FunctionParam
            ? CompletionContext.CalcParamValue
            : CompletionContext.CalcExpression;
        return (scriptCtx, calcItems);
    }
}
