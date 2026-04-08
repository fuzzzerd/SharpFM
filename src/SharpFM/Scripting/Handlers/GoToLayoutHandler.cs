using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class GoToLayoutHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Go to Layout"];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Catalog params: [LayoutDestination(enum), Layout(layout), Animation(enum)].
        // The LayoutDestination label is suppressed in the canonical display and its
        // meaning chooses how to render the rest of the line:
        //   SelectedLayout          → "LayoutName"
        //   OriginalLayout          → original layout
        //   LayoutNameByCalculation → calc expression (pulled from SourceXml — the
        //                              Calculation element isn't modelled as a param)
        //   LayoutNumberByCalculation → Layout Number: calc
        var dest = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "LayoutDestination")?.Value;
        var layoutName = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Layout")?.Value;
        var animation = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Animation")?.Value;

        string? primary = null;
        switch (dest)
        {
            case "OriginalLayout":
                primary = "original layout";
                break;
            case "LayoutNameByCalculation":
            case "LayoutNumberByCalculation":
                var calc = step.SourceXml?.Element("Calculation")?.Value;
                primary = dest == "LayoutNumberByCalculation"
                    ? $"Layout Number: {calc ?? ""}"
                    : calc ?? "";
                break;
            case "SelectedLayout":
            default:
                // Layout type is extracted as quoted: "Name"
                primary = !string.IsNullOrEmpty(layoutName) ? layoutName : null;
                break;
        }

        if (string.IsNullOrEmpty(primary) && string.IsNullOrEmpty(animation))
            return "Go to Layout";

        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(primary)) parts.Add(primary);
        if (!string.IsNullOrEmpty(animation)) parts.Add($"Animation: {animation}");
        return $"Go to Layout [ {string.Join(" ; ", parts)} ]";
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
