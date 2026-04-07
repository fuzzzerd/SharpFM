using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting;

public partial class ScriptStep
{
    public StepDefinition? Definition { get; }
    public bool Enabled { get; set; }
    public List<StepParamValue> ParamValues { get; }

    /// <summary>
    /// Original XML element, kept only for unknown steps (Definition == null)
    /// as a fallback for display and serialization. For known steps, ParamValues
    /// is always the source of truth.
    /// </summary>
    public XElement? SourceXml { get; }

    public ScriptStep(StepDefinition? definition, bool enabled,
                      List<StepParamValue>? paramValues = null, XElement? rawXml = null)
    {
        Definition = definition;
        Enabled = enabled;
        ParamValues = paramValues ?? new List<StepParamValue>();
        SourceXml = rawXml;
    }

    // --- Factory: from XML element ---

    public static ScriptStep FromXml(XElement stepElement)
    {
        var name = stepElement.Attribute("name")?.Value ?? "";
        var enabled = stepElement.Attribute("enable")?.Value != "False";

        var definition = LookupDefinition(name, stepElement.Attribute("id")?.Value);

        if (definition == null)
        {
            // Unknown step — preserve raw XML
            return new ScriptStep(null, enabled, rawXml: new XElement(stepElement));
        }

        // Extract param values from XML using catalog metadata
        var paramValues = definition.Params
            .Select(p => StepParamValue.FromXml(stepElement, p))
            .ToList();

        return new ScriptStep(definition, enabled, paramValues);
    }

    // --- Serialize to XML (always from ParamValues) ---

    public XElement ToXml()
    {
        if (Definition == null)
        {
            // Unknown step — emit as comment preserving original text
            var text = SourceXml?.Element("RawText")?.Value
                ?? SourceXml?.Attribute("name")?.Value
                ?? "Unknown";
            var commentStep = new XElement("Step",
                new XAttribute("enable", Enabled ? "True" : "False"),
                new XAttribute("id", 89),
                new XAttribute("name", "# (comment)"));
            commentStep.Add(new XElement("Text", text));
            return commentStep;
        }

        var name = Definition.Name;
        var id = Definition.Id ?? 89;

        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));

        if (Definition.SelfClosing && ParamValues.All(p => p.Value == null))
            return step;

        foreach (var pv in ParamValues)
        {
            var xml = pv.ToXml();
            if (xml != null) step.Add(xml);
        }

        return step;
    }

    // --- Render to display line (always from ParamValues) ---

    public string ToDisplayLine()
    {
        if (Definition == null)
            return SourceXml?.Attribute("name")?.Value ?? "Unknown";

        // Comments use a special format: # text (no brackets)
        if (Definition.Name == "# (comment)")
        {
            var text = ParamValues.FirstOrDefault()?.Value ?? "";
            return $"# {text}";
        }

        var parts = ParamValues
            .Select(p => p.ToDisplayString())
            .Where(s => s != null)
            .ToList();

        if (parts.Count == 0)
            return Definition.Name;

        return $"{Definition.Name} [ {string.Join(" ; ", parts)} ]";
    }

    // --- Validate ---

    public List<ScriptDiagnostic> Validate(int lineIndex)
    {
        var diagnostics = new List<ScriptDiagnostic>();

        if (Definition == null)
        {
            var name = SourceXml?.Attribute("name")?.Value ?? "Unknown";
            diagnostics.Add(new ScriptDiagnostic(
                lineIndex, 0, name.Length,
                $"Unknown script step: '{name}'",
                DiagnosticSeverity.Error));
            return diagnostics;
        }

        foreach (var pv in ParamValues)
        {
            diagnostics.AddRange(pv.Validate(lineIndex));
        }

        return diagnostics;
    }

    // --- Helpers ---

    private static StepDefinition? LookupDefinition(string name, string? idStr)
    {
        if (StepCatalogLoader.ByName.TryGetValue(name, out var byName))
            return byName;
        if (idStr != null && int.TryParse(idStr, out var id) &&
            StepCatalogLoader.ById.TryGetValue(id, out var byId))
            return byId;
        return null;
    }

}
