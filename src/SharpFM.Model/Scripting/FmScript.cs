using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting;

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

        XDocument doc = XDocument.Parse(xml);

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

    // --- Apply mutation operations ---

    /// <summary>
    /// Apply a single step operation to this script. Returns any errors as a list
    /// (empty list = success).
    /// </summary>
    public IReadOnlyList<string> Apply(ScriptStepOperation op) =>
        op.Action.ToLowerInvariant() switch
        {
            "add" => ApplyAdd(op),
            "update" => ApplyUpdate(op),
            "remove" => ApplyRemove(op),
            "move" => ApplyMove(op),
            _ => [$"Unknown action '{op.Action}'."],
        };

    private List<string> ApplyAdd(ScriptStepOperation op)
    {
        if (op.StepName is null) return ["StepName is required for add operations."];
        if (!StepCatalogLoader.ByName.TryGetValue(op.StepName, out var definition))
            return [$"Unknown step name '{op.StepName}'."];

        var paramValues = definition.Params.Select(p =>
        {
            var paramName = p.HrLabel ?? p.WrapperElement ?? p.XmlElement;
            string? value = null;
            op.Params?.TryGetValue(paramName, out value);
            return new StepParamValue(p, value);
        }).ToList();

        var step = new ScriptStep(definition, op.Enabled ?? true, paramValues);
        var index = op.Index < 0 || op.Index >= Steps.Count ? Steps.Count : op.Index;
        Steps.Insert(index, step);
        return [];
    }

    private List<string> ApplyUpdate(ScriptStepOperation op)
    {
        if (ValidateStepIndex(op.Index) is { } err) return [err];

        var step = Steps[op.Index];
        if (op.Enabled is not null) step.Enabled = op.Enabled.Value;

        if (op.Params is not null)
        {
            foreach (var (name, value) in op.Params)
            {
                var param = step.ParamValues.FirstOrDefault(p =>
                {
                    var paramName = p.Definition.HrLabel ?? p.Definition.WrapperElement ?? p.Definition.XmlElement;
                    return paramName.Equals(name, StringComparison.OrdinalIgnoreCase);
                });
                if (param is not null) param.Value = value;
                else return [$"Parameter '{name}' not found on step '{step.Definition?.Name ?? "unknown"}'."];
            }
        }
        return [];
    }

    private List<string> ApplyRemove(ScriptStepOperation op)
    {
        if (ValidateStepIndex(op.Index) is { } err) return [err];
        Steps.RemoveAt(op.Index);
        return [];
    }

    private List<string> ApplyMove(ScriptStepOperation op)
    {
        if (ValidateStepIndex(op.Index) is { } err) return [err];
        if (op.MoveToIndex is null) return ["MoveToIndex is required for move operations."];
        if (ValidateStepIndex(op.MoveToIndex.Value) is { } destErr)
            return [destErr.Replace("Step index", "MoveToIndex")];

        var step = Steps[op.Index];
        Steps.RemoveAt(op.Index);
        Steps.Insert(op.MoveToIndex.Value, step);
        return [];
    }

    private string? ValidateStepIndex(int index) =>
        index < 0 || index >= Steps.Count
            ? $"Step index {index} out of range (0-{Steps.Count - 1})."
            : null;
}
