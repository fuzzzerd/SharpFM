using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class PerformScriptHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Perform Script"];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Catalog params: [Calculation (parameter), Script (script name)].
        // Canonical display lists the script name first, then "Parameter: ..."
        // only when a parameter calculation is present.
        var calc = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Calculation")?.Value;
        var scriptName = step.ParamValues
            .FirstOrDefault(p => p.Definition.Type == "script")?.Value;

        if (string.IsNullOrEmpty(scriptName) && string.IsNullOrEmpty(calc))
            return "Perform Script";

        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(scriptName)) parts.Add(scriptName);
        if (!string.IsNullOrEmpty(calc)) parts.Add($"Parameter: {calc}");

        return $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        var scriptName = "";
        var param = ExtractLabeled(hrParams, "Parameter") ?? "";

        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (!trimmed.StartsWith("Parameter:", StringComparison.OrdinalIgnoreCase))
                scriptName = XmlHelpers.Unquote(trimmed);
        }

        var step = MakeStep(1, "Perform Script", enabled);
        step.Add(XElement.Parse($"<Calculation><![CDATA[{param}]]></Calculation>"));
        step.Add(new XElement("Script", new XAttribute("id", "0"), new XAttribute("name", scriptName)));
        return step;
    }
}
