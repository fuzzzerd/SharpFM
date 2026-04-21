using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Scripting;

/// <summary>
/// Parses FileMaker script display text into the typed domain model.
/// Each display line is resolved to its typed step POCO through
/// <see cref="StepDisplayFactory"/>. Unknown step names wrap the
/// original text in a <see cref="RawStep"/> so forward-compat content
/// still round-trips through the XML surface.
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

        // Typed POCO fast-path: every known step name is registered via
        // IStepFactory.
        var typed = StepDisplayFactory.TryCreate(raw.StepName, !raw.Disabled, raw.Params);
        if (typed != null) return typed;

        // Unknown step name — preserve the raw display text inside a
        // synthetic <Step><RawText/></Step> so the round-trip is lossless
        // at the XML level even though the display form is frozen.
        var element = new XElement("Step",
            new XAttribute("enable", raw.Disabled ? "False" : "True"),
            new XAttribute("name", raw.StepName),
            new XElement("RawText", raw.RawLine.Trim()));
        return new RawStep(element);
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
