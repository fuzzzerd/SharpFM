using System;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class GoToLayoutHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Go to Layout"];

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
