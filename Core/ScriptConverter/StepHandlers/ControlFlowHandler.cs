using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.StepHandlers;

internal class ControlFlowHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames =>
    [
        "If", "Else If", "Exit Loop If",
        "Else", "End If", "Loop", "End Loop"
    ];

    public string? ToDisplayLine(ScriptStep step)
    {
        var name = step.Definition!.Name;
        return name switch
        {
            "If" or "Else If" or "Exit Loop If" =>
                FormatCondition(name, step.RawXml?.Element("Calculation")?.Value),
            _ => name
        };
    }

    public XElement? ToXml(ScriptStep step)
    {
        var name = step.Definition!.Name;
        return name switch
        {
            "If" => BuildCondition(68, name, step),
            "Else If" => BuildCondition(125, name, step),
            "Exit Loop If" => BuildCondition(72, name, step),
            "Else" => MakeStep(69, name, step.Enabled),
            "End If" => MakeStep(70, name, step.Enabled),
            "Loop" => MakeStep(71, name, step.Enabled),
            "End Loop" => MakeStep(73, name, step.Enabled),
            _ => null
        };
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        return definition.Name switch
        {
            "If" => BuildConditionFromParams(68, "If", enabled, hrParams),
            "Else If" => BuildConditionFromParams(125, "Else If", enabled, hrParams),
            "Exit Loop If" => BuildConditionFromParams(72, "Exit Loop If", enabled, hrParams),
            "Else" => MakeStep(69, "Else", enabled),
            "End If" => MakeStep(70, "End If", enabled),
            "Loop" => MakeStep(71, "Loop", enabled),
            "End Loop" => MakeStep(73, "End Loop", enabled),
            _ => null
        };
    }

    private static string FormatCondition(string name, string? calc)
    {
        return string.IsNullOrEmpty(calc) ? name : $"{name} [ {calc} ]";
    }

    private static XElement BuildCondition(int id, string name, ScriptStep step)
    {
        var el = MakeStep(id, name, step.Enabled);
        var calc = step.RawXml?.Element("Calculation")?.Value ?? "";
        el.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return el;
    }

    private static XElement BuildConditionFromParams(int id, string name, bool enabled, string[] hrParams)
    {
        var el = MakeStep(id, name, enabled);
        var calc = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        el.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return el;
    }
}
