using System;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class GoToLayoutRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Go to Layout"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var dest = step.Element("LayoutDestination")?.Attribute("value")?.Value;
        var layoutName = step.Element("Layout")?.Attribute("name")?.Value;
        var animation = step.Element("Animation")?.Attribute("value")?.Value;

        string layoutRef = dest switch
        {
            "OriginalLayout" => "original layout",
            "LayoutNameByCalculation" =>
                step.Element("Calculation")?.Value ?? "original layout",
            "LayoutNumberByCalculation" =>
                $"Layout Number: {step.Element("Calculation")?.Value ?? ""}",
            _ => !string.IsNullOrEmpty(layoutName) ? $"\"{layoutName}\"" : "original layout"
        };

        var parts = new System.Collections.Generic.List<string> { layoutRef };

        if (!string.IsNullOrEmpty(animation))
            parts.Add($"Animation: {animation}");

        return $"Go to Layout [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string dest = "OriginalLayout";
        string layoutName = "";
        string animation = "";

        foreach (var p in line.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Animation:", StringComparison.OrdinalIgnoreCase))
            {
                animation = trimmed.Substring(10).TrimStart();
            }
            else if (trimmed.StartsWith("Layout Number:", StringComparison.OrdinalIgnoreCase))
            {
                dest = "LayoutNumberByCalculation";
            }
            else if (trimmed == "original layout")
            {
                dest = "OriginalLayout";
            }
            else
            {
                dest = "SelectedLayout";
                layoutName = GenericStepRenderer.Unquote(trimmed);
            }
        }

        var xml = $"<Step enable=\"{enable}\" id=\"6\" name=\"Go to Layout\">"
            + $"<LayoutDestination value=\"{dest}\"/>";

        if (dest == "SelectedLayout")
            xml += $"<Layout id=\"0\" name=\"{GenericStepRenderer.XmlEscape(layoutName)}\"/>";

        if (!string.IsNullOrEmpty(animation))
            xml += $"<Animation value=\"{GenericStepRenderer.XmlEscape(animation)}\"/>";

        xml += "</Step>";
        return xml;
    }
}
