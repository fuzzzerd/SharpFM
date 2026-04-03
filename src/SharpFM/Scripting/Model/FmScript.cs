using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NLog;

namespace SharpFM.Scripting.Model;

public class FmScript
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
        catch (XmlException ex)
        {
            Log.Error(ex, "Failed to parse script XML");
            return new FmScript(new List<ScriptStep>());
        }

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
            if (step.Definition?.BlockPair?.Role is BlockPairRole.Close or BlockPairRole.Middle && indentLevel > 0)
                indentLevel--;

            var displayLine = step.ToDisplayLine();

            // Apply disabled prefix
            if (!step.Enabled)
                displayLine = $"// {displayLine}";

            // Apply indentation
            if (indentLevel > 0)
                displayLine = string.Concat(Enumerable.Repeat(indent, indentLevel)) + displayLine;

            result.Add(displayLine);

            // Increase indent after open/middle blocks
            if (step.Definition?.BlockPair?.Role is BlockPairRole.Open or BlockPairRole.Middle)
                indentLevel++;
        }

        return result.ToArray();
    }

    // --- Validate ---
    // Block pair and positional validation lives in ScriptValidator (needs text line positions).
    // This method validates the model itself (per-step param validation only).

    public List<ScriptDiagnostic> Validate()
    {
        var diagnostics = new List<ScriptDiagnostic>();
        for (int i = 0; i < Steps.Count; i++)
            diagnostics.AddRange(Steps[i].Validate(i));
        return diagnostics;
    }

    // --- Mutation API ---

    /// <summary>Fires after any mutation to the Steps collection.</summary>
    public event EventHandler? StepsChanged;

    /// <summary>Number of steps in the script.</summary>
    public int StepCount => Steps.Count;

    /// <summary>Get a step by index.</summary>
    public ScriptStep GetStep(int index) => Steps[index];

    /// <summary>Insert a step at the given index.</summary>
    public void AddStep(int index, ScriptStep step)
    {
        Steps.Insert(index, step);
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Remove the step at the given index.</summary>
    public void RemoveStep(int index)
    {
        Steps.RemoveAt(index);
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Move a step from one index to another.</summary>
    public void MoveStep(int fromIndex, int toIndex)
    {
        var step = Steps[fromIndex];
        Steps.RemoveAt(fromIndex);
        Steps.Insert(toIndex, step);
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replace the step at the given index.</summary>
    public void UpdateStep(int index, ScriptStep replacement)
    {
        Steps[index] = replacement;
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replace the step at the given index from a display line string.</summary>
    public void UpdateStep(int index, string displayLine)
    {
        if (index < 0 || index >= Steps.Count) return;
        Steps[index] = ScriptStep.FromDisplayLine(displayLine);
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replace all steps (bulk operation, e.g. from re-parsing text or XML).</summary>
    public void ReplaceSteps(IReadOnlyList<ScriptStep> steps)
    {
        Steps.Clear();
        Steps.AddRange(steps);
        StepsChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- Query API ---

    /// <summary>Find all steps matching a step name (case-insensitive).</summary>
    public IReadOnlyList<(int Index, ScriptStep Step)> FindSteps(string stepName)
    {
        var results = new List<(int, ScriptStep)>();
        for (int i = 0; i < Steps.Count; i++)
        {
            if (string.Equals(Steps[i].Definition?.Name, stepName, StringComparison.OrdinalIgnoreCase))
                results.Add((i, Steps[i]));
        }
        return results;
    }

}
