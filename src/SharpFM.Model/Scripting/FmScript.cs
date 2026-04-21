using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Model.Scripting;

public class FmScript
{
    public List<ScriptStep> Steps { get; }

    /// <summary>
    /// Wrapper metadata from a Mac-XMSC ("Script") clipboard payload.
    /// Null for Mac-XMSS ("ScriptSteps") payloads. When non-null,
    /// <see cref="ToXml"/> emits the <c>&lt;Script&gt;</c> wrapper so the
    /// output is paste-compatible with FM Pro under the Mac-XMSC format.
    /// </summary>
    public ScriptMetadata? Metadata { get; set; }

    public FmScript(List<ScriptStep> steps, ScriptMetadata? metadata = null)
    {
        Steps = steps;
        Metadata = metadata;
    }

    // --- Parse FM XML into model ---

    public static FmScript FromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new FmScript(new List<ScriptStep>());

        XDocument doc = XDocument.Parse(xml);

        var root = doc.Root;
        if (root == null) return new FmScript(new List<ScriptStep>());

        // Mac-XMSC: steps inside <Script> wrapper — preserve wrapper attrs.
        // Mac-XMSS: steps directly under <fmxmlsnippet> root — no metadata.
        var script = root.Element("Script");
        ScriptMetadata? metadata = null;
        IEnumerable<XElement> stepElements;
        if (script != null)
        {
            metadata = ScriptMetadata.FromXml(script);
            stepElements = script.Elements("Step");
        }
        else
        {
            stepElements = root.Elements("Step");
        }

        var steps = stepElements.Select(ScriptStep.FromXml).ToList();
        return new FmScript(steps, metadata);
    }

    // --- Serialize to FM XML ---

    public string ToXml()
    {
        var root = new XElement("fmxmlsnippet", new XAttribute("type", "FMObjectList"));

        if (Metadata is not null)
        {
            var scriptEl = Metadata.ToXmlElement();
            foreach (var step in Steps)
                scriptEl.Add(step.ToXml());
            root.Add(scriptEl);
        }
        else
        {
            foreach (var step in Steps)
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

    /// <summary>
    /// One indent level as emitted by <see cref="ToDisplayLines"/>. Exported so
    /// the parser (<see cref="ScriptLineParser"/>) can measure and strip block
    /// indent with the same width used at render time.
    /// </summary>
    internal const string IndentUnit = "    ";

    public string[] ToDisplayLines()
    {
        var result = new List<string>();
        int indentLevel = 0;

        foreach (var step in Steps)
        {
            // Decrease indent before close/middle blocks
            var metadata = Registry.StepRegistry.MetadataFor(step);
            if (metadata?.BlockPair?.Role is BlockPairRole.Close or BlockPairRole.Middle && indentLevel > 0)
                indentLevel--;

            var displayLine = step.ToDisplayLine();

            // Apply disabled prefix
            if (!step.Enabled)
                displayLine = $"// {displayLine}";

            // Apply block indentation
            var blockIndent = indentLevel > 0
                ? string.Concat(Enumerable.Repeat(IndentUnit, indentLevel))
                : string.Empty;
            displayLine = blockIndent + displayLine;

            if (displayLine.Contains('\n'))
            {
                var subLines = displayLine.Split('\n');

                // Align continuation lines to the column immediately after the
                // first line's opening '[ '. Matches FM Pro's convention of
                // treating each step as one "line" — visual breaks for calc
                // newlines sit aligned under the bracket content.
                var firstLine = subLines[0];
                var bracketIdx = firstLine.IndexOf('[');
                var continuationIndent = bracketIdx >= 0
                    ? new string(' ', bracketIdx + 2)
                    : blockIndent;

                result.Add(firstLine);
                for (int i = 1; i < subLines.Length; i++)
                    result.Add(continuationIndent + subLines[i]);
            }
            else
            {
                result.Add(displayLine);
            }

            // Increase indent after open/middle blocks
            if (metadata?.BlockPair?.Role is BlockPairRole.Open or BlockPairRole.Middle)
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
        if (!Registry.StepRegistry.ByName.ContainsKey(op.StepName))
            return [$"Unknown step name '{op.StepName}'."];

        // Route through each POCO's FromDisplay factory with the caller's
        // param map synthesized into labeled HR tokens. POCOs that take
        // unlabeled positional params parse them in order.
        var hrParams = SynthesizeHrParams(op.Params);
        var step = StepDisplayFactory.TryCreate(op.StepName, op.Enabled ?? true, hrParams);
        if (step is null)
            return [$"No typed POCO factory registered for '{op.StepName}'."];

        var index = op.Index < 0 || op.Index >= Steps.Count ? Steps.Count : op.Index;
        Steps.Insert(index, step);
        return [];
    }

    private List<string> ApplyUpdate(ScriptStepOperation op)
    {
        if (ValidateStepIndex(op.Index) is { } err) return [err];

        var step = Steps[op.Index];
        if (op.Enabled is not null) step.Enabled = op.Enabled.Value;

        if (op.Params is null) return [];

        // Param updates are rebuilt by re-parsing the display form with the
        // new param map overlaid onto the old. This uses the POCO's own
        // FromDisplay factory — the same path ApplyAdd takes — so the
        // update never leaves the typed-POCO world.
        var metadata = Registry.StepRegistry.MetadataFor(step);
        if (metadata is null)
            return [$"Apply/update is not supported for step kind '{step.GetType().Name}'."];

        var hrParams = SynthesizeHrParams(op.Params);
        var updated = StepDisplayFactory.TryCreate(metadata.Name, step.Enabled, hrParams);
        if (updated is null)
            return [$"No typed POCO factory registered for '{metadata.Name}'."];

        Steps[op.Index] = updated;
        return [];
    }

    private static string[] SynthesizeHrParams(IReadOnlyDictionary<string, string>? map)
    {
        if (map is null || map.Count == 0) return [];
        var result = new List<string>(map.Count);
        foreach (var (k, v) in map)
        {
            // Bare values (key is empty) are positional tokens; labeled
            // entries become "Label: value" tokens which each POCO's
            // FromDisplay parser recognizes.
            result.Add(string.IsNullOrEmpty(k) ? v : $"{k}: {v}");
        }
        return result.ToArray();
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
