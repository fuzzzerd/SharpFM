using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Scripting;

/// <summary>
/// Parses FileMaker script display text into the typed domain model.
/// This is a UI concern — text enters from the editor, tokens come out
/// of <see cref="ScriptLineParser"/>, and each line is resolved to either
/// a typed step POCO (via <see cref="StepDisplayFactory"/>) or a
/// <see cref="RawStep"/> wrapping a freshly built catalog-driven
/// <see cref="XElement"/>.
/// </summary>
public static class ScriptTextParser
{
    public static ScriptStep FromDisplayLine(string line)
    {
        var raw = ScriptLineParser.ParseRaw(line);

        // Comments are recognized structurally by ScriptLineParser and
        // always land as a RawStep with a <Text> child — their display
        // form is a single "# text" line with no brackets.
        if (raw.IsComment)
        {
            return BuildCommentRawStep(!raw.Disabled, raw.Params.Length > 0 ? raw.Params[0] : "");
        }

        if (!StepCatalogLoader.ByName.TryGetValue(raw.StepName, out var definition))
        {
            // Non-catalog step: wrap the original text in a synthetic
            // <Step name="X"><RawText>...</RawText></Step> so the raw
            // editor content survives through the XML round-trip.
            var element = new XElement("Step",
                new XAttribute("enable", raw.Disabled ? "False" : "True"),
                new XAttribute("name", raw.StepName),
                new XElement("RawText", raw.RawLine.Trim()));
            return new RawStep(element, null);
        }

        // Typed POCO fast-path: if a migrated step is registered for this
        // name, let it parse the display tokens directly.
        var typed = StepDisplayFactory.TryCreate(definition.Name, !raw.Disabled, raw.Params);
        if (typed != null) return typed;

        // Generic fallback: build an XElement from catalog metadata + the
        // matched display tokens and wrap it in a RawStep.
        var stepElement = CatalogXmlBuilder.BuildStep(definition, !raw.Disabled, raw.Params);
        return new RawStep(stepElement, definition);
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

    private static RawStep BuildCommentRawStep(bool enabled, string text)
    {
        var def = StepCatalogLoader.ByName["# (comment)"];
        var element = new XElement("Step",
            new XAttribute("enable", enabled ? "True" : "False"),
            new XAttribute("id", def.Id ?? 89),
            new XAttribute("name", "# (comment)"),
            new XElement("Text", text));
        return new RawStep(element, def);
    }

    /// <summary>
    /// Merge consecutive comment steps into a single comment so that
    /// multi-line comments in the display editor survive the round-trip
    /// without being split. Operates on the XElement carried by each
    /// RawStep (comments are still RawStep-backed until a typed
    /// CommentStep POCO arrives in Phase 3).
    /// </summary>
    private static List<ScriptStep> MergeCommentContinuations(List<ScriptStep> steps)
    {
        var result = new List<ScriptStep>();

        foreach (var step in steps)
        {
            bool isComment = step.Definition?.Name == "# (comment)";
            bool prevIsComment = result.Count > 0 && result[^1].Definition?.Name == "# (comment)";

            if (prevIsComment && isComment && result[^1] is RawStep prev && step is RawStep current)
            {
                var prevText = prev.ToXml().Element("Text")?.Value ?? "";
                var thisText = current.ToXml().Element("Text")?.Value ?? "";
                var merged = string.IsNullOrEmpty(prevText) ? thisText : prevText + "\n" + thisText;

                // Comments are immutable RawSteps; rebuild the merged
                // comment from scratch and replace the tail of the list.
                result[^1] = BuildCommentRawStep(prev.Enabled, merged);
            }
            else
            {
                result.Add(step);
            }
        }

        return result;
    }
}
