using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class ControlFlowRenderer : IMultiStepRenderer
{
    public string[] StepNames =>
    [
        "If", "Else If", "Else", "End If",
        "Loop", "Exit Loop If", "End Loop"
    ];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var name = definition.Name;

        switch (name)
        {
            case "If":
            case "Else If":
            case "Exit Loop If":
                var calc = step.Element("Calculation")?.Value;
                return string.IsNullOrEmpty(calc)
                    ? name
                    : $"{name} [ {calc} ]";

            case "Else":
            case "End If":
            case "Loop":
            case "End Loop":
                return name;

            default:
                return name;
        }
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        var name = definition.Name;

        switch (name)
        {
            case "If":
            {
                var calc = line.Params.Length > 0 ? line.Params[0].Trim() : "";
                return $"<Step enable=\"{enable}\" id=\"68\" name=\"If\">"
                    + $"<Calculation><![CDATA[{calc}]]></Calculation>"
                    + "</Step>";
            }
            case "Else If":
            {
                var calc = line.Params.Length > 0 ? line.Params[0].Trim() : "";
                return $"<Step enable=\"{enable}\" id=\"125\" name=\"Else If\">"
                    + $"<Calculation><![CDATA[{calc}]]></Calculation>"
                    + "</Step>";
            }
            case "Exit Loop If":
            {
                var calc = line.Params.Length > 0 ? line.Params[0].Trim() : "";
                return $"<Step enable=\"{enable}\" id=\"72\" name=\"Exit Loop If\">"
                    + $"<Calculation><![CDATA[{calc}]]></Calculation>"
                    + "</Step>";
            }
            case "Else":
                return $"<Step enable=\"{enable}\" id=\"69\" name=\"Else\"/>";
            case "End If":
                return $"<Step enable=\"{enable}\" id=\"70\" name=\"End If\"/>";
            case "Loop":
                return $"<Step enable=\"{enable}\" id=\"71\" name=\"Loop\"/>";
            case "End Loop":
                return $"<Step enable=\"{enable}\" id=\"73\" name=\"End Loop\"/>";
            default:
                return $"<Step enable=\"{enable}\" id=\"{definition.Id ?? 0}\" name=\"{GenericStepRenderer.XmlEscape(name)}\"/>";
        }
    }
}
