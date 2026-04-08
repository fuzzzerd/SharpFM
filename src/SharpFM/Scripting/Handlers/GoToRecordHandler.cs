using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class GoToRecordHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Go to Record/Request/Page"];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Catalog params: [RowPageLocation(enum), Exit(boolean, "Exit after last"),
        //                  NoInteract(boolean, "With dialog", inverted), Calculation(calc)].
        // The canonical display shows the positional location (First/Next/...) and
        // only appends "Exit after last: On" or "With dialog: Off" when those flags
        // are actually engaged. For "By Calculation" the calc replaces the position.
        string? location = null, exit = null, withDialog = null, calc = null;
        foreach (var pv in step.ParamValues)
        {
            switch (pv.Definition.XmlElement)
            {
                case "RowPageLocation": location = pv.Value; break;
                case "Exit": exit = pv.Value; break;
                case "NoInteract": withDialog = pv.Value; break;
                case "Calculation": calc = pv.Value; break;
            }
        }

        var parts = new System.Collections.Generic.List<string>();
        if (location == "By Calculation")
        {
            if (!string.IsNullOrEmpty(calc)) parts.Add($"By Calculation: {calc}");
            else parts.Add("By Calculation");
        }
        else if (!string.IsNullOrEmpty(location))
        {
            parts.Add(location);
        }

        // Only surface flags in their non-default engaged form. Defaults are
        // hidden entirely so a round-trip through the default-populated XML
        // doesn't accumulate noise on the display line.
        if (string.Equals(exit, "On", StringComparison.OrdinalIgnoreCase))
            parts.Add("Exit after last: On");
        if (string.Equals(withDialog, "On", StringComparison.OrdinalIgnoreCase))
            parts.Add("With dialog: On");

        if (parts.Count == 0)
            return "Go to Record/Request/Page";

        return $"Go to Record/Request/Page [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        string location = "Next", exitState = "False", calc = "";
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
                exitState = trimmed.Substring(16).TrimStart().Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
            else if (trimmed.StartsWith("By Calculation:", StringComparison.OrdinalIgnoreCase))
            {
                location = "By Calculation";
                calc = trimmed.Substring(15).TrimStart();
            }
            else if (trimmed is "First" or "Last" or "Previous" or "Next")
                location = trimmed;
        }
        var step = MakeStep(16, "Go to Record/Request/Page", enabled);
        step.Add(new XElement("RowPageLocation", new XAttribute("value", location)));
        step.Add(new XElement("Exit", new XAttribute("state", exitState)));
        if (!string.IsNullOrEmpty(calc))
            step.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return step;
    }
}
