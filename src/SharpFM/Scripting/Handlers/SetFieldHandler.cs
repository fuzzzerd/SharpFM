using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class SetFieldHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Set Field"];

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        string fieldTable = "", fieldName = "", calcValue = "";
        if (hrParams.Length >= 1)
        {
            var first = hrParams[0].Trim();
            if (first.Contains("::"))
            {
                var parts = first.Split("::", 2);
                fieldTable = parts[0];
                fieldName = parts[1];
            }
            else fieldName = first;
        }
        if (hrParams.Length >= 2) calcValue = hrParams[1].Trim();

        var step = MakeStep(76, "Set Field", enabled);
        step.Add(XElement.Parse($"<Calculation><![CDATA[{calcValue}]]></Calculation>"));
        step.Add(new XElement("Field",
            new XAttribute("table", fieldTable),
            new XAttribute("id", "0"),
            new XAttribute("name", fieldName)));
        return step;
    }
}
