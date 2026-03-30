using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.StepHandlers;

internal class PerformScriptHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Perform Script"];

    public string? ToDisplayLine(ScriptStep step)
    {
        var scriptName = step.SourceXml?.Element("Script")?.Attribute("name")?.Value;
        var param = step.SourceXml?.Element("Calculation")?.Value;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(scriptName)) parts.Add($"\"{scriptName}\"");
        if (!string.IsNullOrEmpty(param)) parts.Add($"Parameter: {param}");

        return parts.Count == 0 ? "Perform Script" : $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? ToXml(ScriptStep step)
    {
        return BuildXmlFromDisplay(step.Definition!, step.Enabled,
            ScriptLineParser.ParseRaw(step.ToDisplayLine()).Params);
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
