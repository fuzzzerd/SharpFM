using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Scripting.Handlers;

namespace SharpFM.Scripting;

/// <summary>
/// Parses FileMaker script display text into domain model objects.
/// This is a UI editing concern — it uses specialized handlers for
/// steps with tricky syntax (Set Variable, Show Custom Dialog, etc.).
/// </summary>
public static class ScriptTextParser
{
    public static ScriptStep FromDisplayLine(string line)
    {
        var raw = ScriptLineParser.ParseRaw(line);

        if (raw.IsComment)
        {
            var def = StepCatalogLoader.ByName["# (comment)"];
            var textParam = def.Params.FirstOrDefault(p => p.XmlElement == "Text");
            var paramValues = textParam != null
                ? new List<StepParamValue> { new(textParam, raw.Params.Length > 0 ? raw.Params[0] : "") }
                : new List<StepParamValue>();
            return new ScriptStep(def, !raw.Disabled, paramValues);
        }

        if (!StepCatalogLoader.ByName.TryGetValue(raw.StepName, out var definition))
        {
            return new ScriptStep(null, !raw.Disabled, rawXml:
                new XElement("Step",
                    new XAttribute("enable", raw.Disabled ? "False" : "True"),
                    new XAttribute("name", raw.StepName),
                    new XElement("RawText", raw.RawLine.Trim())));
        }

        // Specialized steps build their own XML from display text,
        // then parse that XML to extract ParamValues consistently.
        var specializedXml = StepHandlerRegistry.Get(definition.Name)
            ?.BuildXmlFromDisplay(definition, !raw.Disabled, raw.Params);
        if (specializedXml != null)
            return ScriptStep.FromXml(specializedXml);

        // Generic: match params positionally and by label
        return new ScriptStep(definition, !raw.Disabled,
            MatchDisplayParams(raw.Params, definition));
    }

    public static FmScript FromDisplayText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new FmScript(new List<ScriptStep>());

        var rawLines = text.Split('\n');
        var mergedLines = ScriptLineParser.MergeMultilineStatements(rawLines);

        var steps = new List<ScriptStep>();
        foreach (var line in mergedLines)
        {
            var trimmed = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
            steps.Add(FromDisplayLine(trimmed));
        }

        steps = MergeCommentContinuations(steps);
        return new FmScript(steps);
    }

    public static void UpdateStep(FmScript script, int index, string displayLine)
    {
        if (index < 0 || index >= script.Steps.Count) return;
        script.Steps[index] = FromDisplayLine(displayLine);
    }

    private static List<StepParamValue> MatchDisplayParams(string[] hrParams, StepDefinition definition)
    {
        var result = new List<StepParamValue>();
        var used = new bool[hrParams.Length];

        foreach (var paramDef in definition.Params)
        {
            var label = paramDef.HrLabel
                ?? (paramDef.Type == "namedCalc" && paramDef.WrapperElement != null
                    ? paramDef.WrapperElement : null);

            string? value = null;

            if (label != null)
            {
                for (int i = 0; i < hrParams.Length; i++)
                {
                    if (used[i]) continue;
                    var trimmed = hrParams[i].TrimStart();
                    if (trimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        value = trimmed.Substring(label.Length + 1).TrimStart();
                        used[i] = true;
                        break;
                    }
                }
            }

            if (value == null)
            {
                for (int i = 0; i < hrParams.Length; i++)
                {
                    if (used[i]) continue;
                    value = hrParams[i].Trim();
                    used[i] = true;
                    break;
                }
            }

            result.Add(new StepParamValue(paramDef, value));
        }

        return result;
    }

    private static List<ScriptStep> MergeCommentContinuations(List<ScriptStep> steps)
    {
        var result = new List<ScriptStep>();

        foreach (var step in steps)
        {
            bool isComment = step.Definition?.Name == "# (comment)";
            bool prevIsComment = result.Count > 0 && result[^1].Definition?.Name == "# (comment)";

            if (prevIsComment && isComment)
            {
                var prev = result[^1];
                var prevText = prev.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Text")?.Value ?? "";
                var thisText = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Text")?.Value ?? "";
                var merged = string.IsNullOrEmpty(prevText) ? thisText : prevText + "\n" + thisText;

                var textParam = prev.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Text");
                if (textParam != null) textParam.Value = merged;
            }
            else
            {
                result.Add(step);
            }
        }

        return result;
    }
}
