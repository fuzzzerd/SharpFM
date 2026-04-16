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

        // Comments are recognized structurally by ScriptLineParser. Hand
        // them to the typed CommentStep display factory — it knows how to
        // unwrap the ⏎ glyph back to a literal newline in Text.
        if (raw.IsComment)
        {
            var typedComment = StepDisplayFactory.TryCreate("# (comment)", !raw.Disabled, raw.Params);
            if (typedComment != null) return typedComment;
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
        // Only the truly-empty string maps to an empty script. A string
        // that's just whitespace / newlines is a script of empty-comment
        // steps (one per blank line) — FM Pro's model for blank-line
        // spacers in the script editor.
        if (string.IsNullOrEmpty(text))
            return new FmScript(new List<ScriptStep>());

        var rawLines = text.Split('\n');
        var mergedLines = ScriptLineParser.MergeMultilineStatements(rawLines);

        var steps = new List<ScriptStep>();
        foreach (var line in mergedLines)
        {
            var trimmed = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // FM Pro convention: a blank line in the script editor is
                // a <Step id="89"> with empty Text. SharpFM preserves that
                // by emitting an empty CommentStep for every blank display
                // line — matches FM Pro's script model exactly.
                steps.Add(new CommentStep(enabled: true, text: string.Empty));
                continue;
            }
            steps.Add(FromDisplayLine(trimmed));
        }

        return new FmScript(steps);
    }

    public static void UpdateStep(FmScript script, int index, string displayLine)
    {
        if (index < 0 || index >= script.Steps.Count) return;
        script.Steps[index] = FromDisplayLine(displayLine);
    }
}
