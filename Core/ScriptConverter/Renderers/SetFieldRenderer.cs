using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class SetFieldRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Set Field"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var field = step.Element("Field");
        var calc = step.Element("Calculation")?.Value;

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

        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(fieldRef))
            parts.Add(fieldRef);
        if (!string.IsNullOrEmpty(calc))
            parts.Add(calc);

        if (parts.Count == 0)
            return "Set Field";

        return $"Set Field [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string fieldTable = "";
        string fieldName = "";
        string calcValue = "";

        if (line.Params.Length >= 1)
        {
            var firstParam = line.Params[0].Trim();
            if (firstParam.Contains("::"))
            {
                var parts = firstParam.Split("::", 2);
                fieldTable = parts[0];
                fieldName = parts[1];
            }
            else
            {
                fieldName = firstParam;
            }
        }

        if (line.Params.Length >= 2)
            calcValue = line.Params[1].Trim();

        return $"<Step enable=\"{enable}\" id=\"76\" name=\"Set Field\">"
            + $"<Calculation><![CDATA[{calcValue}]]></Calculation>"
            + $"<Field table=\"{GenericStepRenderer.XmlEscape(fieldTable)}\" id=\"0\" name=\"{GenericStepRenderer.XmlEscape(fieldName)}\"/>"
            + "</Step>";
    }
}
