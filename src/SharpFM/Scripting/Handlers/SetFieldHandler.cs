using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class SetFieldHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Set Field"];

    public string? ToDisplayLine(ScriptStep step)
    {
        var field = step.SourceXml?.Element("Field");
        string fieldRef = "";
        if (field != null)
        {
            var table = field.Attribute("table")?.Value;
            var name = field.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(name))
                fieldRef = $"{table}::{name}";
            else if (!string.IsNullOrEmpty(name))
                fieldRef = name;
            else if (!string.IsNullOrEmpty(field.Value))
                fieldRef = field.Value;
        }

        var calc = step.SourceXml?.Element("Calculation")?.Value;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(fieldRef)) parts.Add(fieldRef);
        if (!string.IsNullOrEmpty(calc)) parts.Add(calc);

        return parts.Count == 0 ? "Set Field" : $"Set Field [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? ToXml(ScriptStep step)
    {
        return BuildXmlFromDisplay(step.Definition!, step.Enabled,
            ScriptLineParser.ParseRaw(step.ToDisplayLine()).Params);
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
