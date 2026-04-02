using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class GoToLayoutHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Go to Layout"];

    public string? ToDisplayLine(ScriptStep step)
    {
        var dest = step.SourceXml?.Element("LayoutDestination")?.Attribute("value")?.Value;
        var layoutName = step.SourceXml?.Element("Layout")?.Attribute("name")?.Value;
        var animation = step.SourceXml?.Element("Animation")?.Attribute("value")?.Value;

        string layoutRef = dest switch
        {
            "OriginalLayout" => "original layout",
            "LayoutNameByCalculation" => step.SourceXml?.Element("Calculation")?.Value ?? "original layout",
            "LayoutNumberByCalculation" => $"Layout Number: {step.SourceXml?.Element("Calculation")?.Value ?? ""}",
            _ => !string.IsNullOrEmpty(layoutName) ? $"\"{layoutName}\"" : "original layout"
        };

        var parts = new List<string> { layoutRef };
        if (!string.IsNullOrEmpty(animation)) parts.Add($"Animation: {animation}");

        return $"Go to Layout [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? ToXml(ScriptStep step)
    {
        return BuildXmlFromDisplay(step.Definition!, step.Enabled,
            ScriptLineParser.ParseRaw(step.ToDisplayLine()).Params);
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        string dest = "OriginalLayout", layoutName = "";
        var animation = ExtractLabeled(hrParams, "Animation");

        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Animation:", StringComparison.OrdinalIgnoreCase)) continue;
            if (trimmed.StartsWith("Layout Number:", StringComparison.OrdinalIgnoreCase))
                dest = "LayoutNumberByCalculation";
            else if (trimmed == "original layout")
                dest = "OriginalLayout";
            else
            {
                dest = "SelectedLayout";
                layoutName = XmlHelpers.Unquote(trimmed);
            }
        }

        var step = MakeStep(6, "Go to Layout", enabled);
        step.Add(new XElement("LayoutDestination", new XAttribute("value", dest)));
        if (dest == "SelectedLayout")
            step.Add(new XElement("Layout", new XAttribute("id", "0"), new XAttribute("name", layoutName)));
        if (!string.IsNullOrEmpty(animation))
            step.Add(new XElement("Animation", new XAttribute("value", animation)));
        return step;
    }
}
