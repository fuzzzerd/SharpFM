using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform Script on Server with Callback. Same target/parameter shape as
/// Perform Script on Server, plus a CallbackScriptState enum and an
/// optional CallbackScript block carrying the callback's script ref and
/// parameter.
/// </summary>
public sealed class PerformScriptOnServerWithCallbackStep : ScriptStep, IStepFactory
{
    public const int XmlId = 210;
    public const string XmlName = "Perform Script on Server with Callback";

    public string State { get; set; }
    public PerformScriptTarget Target { get; set; }
    public Calculation? Parameter { get; set; }
    public NamedRef? CallbackScript { get; set; }
    public Calculation? CallbackParameter { get; set; }

    public PerformScriptOnServerWithCallbackStep(
        string state = "Continue",
        PerformScriptTarget? target = null,
        Calculation? parameter = null,
        NamedRef? callbackScript = null,
        Calculation? callbackParameter = null,
        bool enabled = true)
        : base(enabled)
    {
        State = state;
        Target = target ?? new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        Parameter = parameter;
        CallbackScript = callbackScript;
        CallbackParameter = callbackParameter;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                step.Add(new XElement("CallbackScriptState", new XAttribute("value", State)));
                if (Parameter is not null) step.Add(Parameter.ToXml());
                step.Add(byRef.Script.ToXml("Script"));
                break;
            case PerformScriptTarget.ByCalculation byCalc:
                step.Add(new XElement("Calculated", byCalc.NameCalc.ToXml()));
                step.Add(new XElement("CallbackScriptState", new XAttribute("value", State)));
                if (Parameter is not null) step.Add(Parameter.ToXml());
                break;
        }

        if (CallbackScript is not null)
        {
            var cb = new XElement("CallbackScript", CallbackScript.ToXml("ScriptName"));
            if (CallbackParameter is not null)
                cb.Add(new XElement("ScriptParameter", CallbackParameter.ToXml("Calculation")));
            step.Add(cb);
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string> { $"State: {State}" };
        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                parts.Add(byRef.Script.Id == 0 ? $"\"{byRef.Script.Name}\"" : $"\"{byRef.Script.Name}\" (#{byRef.Script.Id})");
                break;
            case PerformScriptTarget.ByCalculation byCalc:
                parts.Add($"By name: {byCalc.NameCalc.Text}");
                break;
        }
        if (Parameter is not null) parts.Add($"Parameter: {Parameter.Text}");
        if (CallbackScript is not null) parts.Add($"Callback: \"{CallbackScript.Name}\"");
        return $"Perform Script on Server with Callback [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("CallbackScriptState")?.Attribute("value")?.Value ?? "Continue";
        var calcEl = step.Element("Calculated")?.Element("Calculation");
        var scriptEl = step.Element("Script");
        PerformScriptTarget target = calcEl is not null
            ? new PerformScriptTarget.ByCalculation(Calculation.FromXml(calcEl))
            : scriptEl is not null
                ? new PerformScriptTarget.ByReference(NamedRef.FromXml(scriptEl))
                : new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        var paramEl = step.Element("Calculation");
        var parameter = paramEl is not null ? Calculation.FromXml(paramEl) : null;
        var cbEl = step.Element("CallbackScript");
        var cbScript = cbEl?.Element("ScriptName") is { } sn ? NamedRef.FromXml(sn) : null;
        var cbParam = cbEl?.Element("ScriptParameter")?.Element("Calculation") is { } cp ? Calculation.FromXml(cp) : null;
        return new PerformScriptOnServerWithCallbackStep(state, target, parameter, cbScript, cbParam, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display is lossy for the callback details. Best-effort parse of state + target.
        string state = "Continue";
        PerformScriptTarget target = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("State:", System.StringComparison.OrdinalIgnoreCase))
                state = t.Substring(6).Trim();
        }
        return new PerformScriptOnServerWithCallbackStep(state, target, null, null, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-script-on-server-callback.html",
        Params =
        [
            new ParamMetadata { Name = "CallbackScriptState", XmlElement = "CallbackScriptState", XmlAttr = "value", Type = "enum", HrLabel = "State", ValidValues = ["Continue", "Halt", "Exit", "Resume", "Pause"], DefaultValue = "Continue" },
            new ParamMetadata { Name = "Script", XmlElement = "Script", Type = "script" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "Parameter" },
            new ParamMetadata { Name = "CallbackScript", XmlElement = "CallbackScript", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
