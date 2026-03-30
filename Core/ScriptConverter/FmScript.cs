using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public class FmScript
{
    public List<ScriptStep> Steps { get; }

    public FmScript(List<ScriptStep> steps)
    {
        Steps = steps;
    }

    // --- Parse FM XML into model ---

    public static FmScript FromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new FmScript(new List<ScriptStep>());

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch { return new FmScript(new List<ScriptStep>()); }

        var root = doc.Root;
        if (root == null) return new FmScript(new List<ScriptStep>());

        // Mac-XMSC: steps inside <Script> wrapper
        var script = root.Element("Script");
        var stepElements = script != null
            ? script.Elements("Step")
            : root.Elements("Step");

        var steps = stepElements.Select(ScriptStep.FromXml).ToList();
        return new FmScript(steps);
    }

    // --- Parse display text into model ---

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
            steps.Add(ScriptStep.FromDisplayLine(trimmed));
        }

        // Merge consecutive comment lines into single comment steps
        steps = MergeCommentContinuations(steps);
        return new FmScript(steps);
    }

    // --- Serialize to FM XML ---

    public string ToXml()
    {
        var root = new XElement("fmxmlsnippet", new XAttribute("type", "FMObjectList"));

        foreach (var step in Steps)
        {
            root.Add(step.ToXml());
        }

        return XmlHelpers.PrettyPrint(root.ToString());
    }

    // --- Render to display text ---

    public string ToDisplayText()
    {
        var lines = ToDisplayLines();
        return string.Join("\n", lines);
    }

    public string[] ToDisplayLines()
    {
        var result = new List<string>();
        int indentLevel = 0;
        const string indent = "    ";

        foreach (var step in Steps)
        {
            var name = step.Definition?.Name ?? "";

            // Decrease indent before close/middle blocks
            if (step.Definition?.BlockPair?.Role is "close" or "middle" && indentLevel > 0)
                indentLevel--;

            var displayLine = step.ToDisplayLine();

            // Apply disabled prefix
            if (!step.Enabled)
                displayLine = $"// {displayLine}";

            // Apply indentation
            if (indentLevel > 0)
                displayLine = string.Concat(Enumerable.Repeat(indent, indentLevel)) + displayLine;

            // Comments may produce multi-line output
            if (displayLine.Contains('\n'))
            {
                foreach (var subLine in displayLine.Split('\n'))
                    result.Add(subLine);
            }
            else
            {
                result.Add(displayLine);
            }

            // Increase indent after open/middle blocks
            if (step.Definition?.BlockPair?.Role is "open" or "middle")
                indentLevel++;
        }

        return result.ToArray();
    }

    // --- Validate ---

    public List<ScriptDiagnostic> Validate()
    {
        var diagnostics = new List<ScriptDiagnostic>();
        var blockStack = new Stack<(string StepName, int Line)>();

        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];

            // Per-step validation
            diagnostics.AddRange(step.Validate(i));

            // Block pair validation
            if (step.Definition?.BlockPair != null)
            {
                switch (step.Definition.BlockPair.Role)
                {
                    case "open":
                        blockStack.Push((step.Definition.Name, i));
                        break;
                    case "middle":
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(i, 0, step.Definition.Name.Length,
                                $"'{step.Definition.Name}' without matching opening step",
                                DiagnosticSeverity.Error));
                        break;
                    case "close":
                        if (blockStack.Count == 0)
                            diagnostics.Add(new ScriptDiagnostic(i, 0, step.Definition.Name.Length,
                                $"'{step.Definition.Name}' without matching opening step",
                                DiagnosticSeverity.Error));
                        else
                            blockStack.Pop();
                        break;
                }
            }
        }

        while (blockStack.Count > 0)
        {
            var unclosed = blockStack.Pop();
            diagnostics.Add(new ScriptDiagnostic(unclosed.Line, 0, 0,
                $"'{unclosed.StepName}' has no matching closing step",
                DiagnosticSeverity.Error));
        }

        return diagnostics;
    }

    // --- Update single step from edited display line ---

    public void UpdateStep(int index, string displayLine)
    {
        if (index < 0 || index >= Steps.Count) return;
        Steps[index] = ScriptStep.FromDisplayLine(displayLine);
    }

    // --- Comment merging ---

    private static List<ScriptStep> MergeCommentContinuations(List<ScriptStep> steps)
    {
        var result = new List<ScriptStep>();

        foreach (var step in steps)
        {
            bool isComment = step.Definition?.Name == "# (comment)";
            bool isBareText = !isComment && step.Definition?.Name == "# (comment)"
                && step.ParamValues.Any(p => p.Value?.StartsWith("[Unknown]") == false);

            // Bare unknown text following a comment → merge
            bool prevIsComment = result.Count > 0 && result[^1].Definition?.Name == "# (comment)";

            if (prevIsComment && isComment)
            {
                // Consecutive comments: merge text
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
