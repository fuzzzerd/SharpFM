using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Scripting.Model;

public partial class ScriptStep
{
    public StepDefinition? Definition { get; }
    public bool Enabled { get; set; }
    public List<StepParamValue> ParamValues { get; }
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

        return new ScriptStep(definition, enabled, paramValues, new XElement(stepElement));
    }

    // --- Factory: from display line text ---

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
            // Unknown step — preserve as-is with Definition=null.
            // Validate() will flag it. ToXml() will emit it as a comment for safety.
            return new ScriptStep(null, !raw.Disabled, rawXml:
                new XElement("Step",
                    new XAttribute("enable", raw.Disabled ? "False" : "True"),
                    new XAttribute("name", raw.StepName),
                    new XElement("RawText", raw.RawLine.Trim())));
        }

        // Specialized steps build their own XML from display text,
        // then construct from that XML for consistent SourceXml population.
        var specializedXml = BuildXmlFromDisplay_Specialized(definition, !raw.Disabled, raw.Params);
        if (specializedXml != null)
            return FromXml(specializedXml);

        // Generic: match params positionally and by label
        return new ScriptStep(definition, !raw.Disabled,
            MatchDisplayParams(raw.Params, definition));
    }

    // --- Serialize to XML ---

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

        var name = Definition?.Name ?? "# (comment)";
        var id = Definition?.Id ?? 89;

        // Check for specialized serialization
        var specialized = ToXml_Specialized();
        if (specialized != null) return specialized;

        // Generic serialization from param values
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));

        if (Definition?.SelfClosing == true && ParamValues.All(p => p.Value == null))
            return step;

        foreach (var pv in ParamValues)
        {
            var xml = pv.ToXml();
            if (xml != null) step.Add(xml);
        }

        return step;
    }

    // --- Render to display line ---

    public string ToDisplayLine()
    {
        // Check for specialized display
        var specialized = ToDisplayLine_Specialized();
        if (specialized != null)
            return specialized;

        // Generic: StepName [ param1 ; param2 ]
        var parts = ParamValues
            .Select(p => p.ToDisplayString())
            .Where(s => s != null)
            .ToList();

        var name = Definition?.Name ?? SourceXml?.Attribute("name")?.Value ?? "Unknown";

        if (parts.Count == 0)
            return name;

        return $"{name} [ {string.Join(" ; ", parts)} ]";
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

        // Per-param validation
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

            // First: try label matching
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

            // Second: positional fill if no label match
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
}
