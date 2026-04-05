using System;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class GoToRecordHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Go to Record/Request/Page"];

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
