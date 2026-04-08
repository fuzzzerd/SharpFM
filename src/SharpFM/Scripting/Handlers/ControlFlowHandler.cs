using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class ControlFlowHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames =>
    [
        "If",
        "Else If",
        "Exit Loop If",
        "Else",
        "End If",
        "Loop",
        "End Loop"
    ];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Control-flow steps only surface the Calculation (condition). The Restore
        // boolean on If/Else If is internal plumbing and must never appear in the
        // canonical display — even when the param has round-tripped to its default.
        if (step.Definition == null) return null;

        var calc = step.ParamValues
            .FirstOrDefault(p => p.Definition.XmlElement == "Calculation")?.Value;

        if (string.IsNullOrEmpty(calc))
            return step.Definition.Name;

        return $"{step.Definition.Name} [ {calc} ]";
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

    private static XElement BuildConditionFromParams(int id, string name, bool enabled, string[] hrParams)
    {
        var el = MakeStep(id, name, enabled);
        var calc = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        el.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return el;
    }
}
