using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class SetFieldHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Set Field"];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Catalog order: [Calculation, Field]. Canonical display swaps them:
        // Field appears first, Calculation second.
        var calc = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Calculation")?.Value;
        var field = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Field")?.Value;

        if (string.IsNullOrEmpty(field) && string.IsNullOrEmpty(calc))
            return "Set Field";

        if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(calc))
            return $"Set Field [ {field} ; {calc} ]";

        if (!string.IsNullOrEmpty(field))
            return $"Set Field [ {field} ]";

        return $"Set Field [ {calc} ]";
    }

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
