using System;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class PerformScriptHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Perform Script"];

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
