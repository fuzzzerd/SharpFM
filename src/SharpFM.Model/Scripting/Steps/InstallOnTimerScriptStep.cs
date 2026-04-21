using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Install OnTimer Script carries a Script ref and an optional Interval
/// calculation wrapped in an <c>&lt;Interval&gt;</c> element. Omitting
/// both cancels any running timer on the current window.
/// </summary>
public sealed class InstallOnTimerScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 148;
    public const string XmlName = "Install OnTimer Script";

    public NamedRef? Script { get; set; }
    public Calculation? Interval { get; set; }

    public InstallOnTimerScriptStep(NamedRef? script = null, Calculation? interval = null, bool enabled = true)
        : base(null, enabled)
    {
        Script = script;
        Interval = interval;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (Script is not null) step.Add(Script.ToXml("Script"));
        if (Interval is not null) step.Add(new XElement("Interval", Interval.ToXml("Calculation")));
        return step;
    }

    public override string ToDisplayLine()
    {
        var script = Script is null ? "<no script>" : $"\"{Script.Name}\"";
        return Interval is null
            ? $"Install OnTimer Script [ {script} ]"
            : $"Install OnTimer Script [ {script} ; Interval: {Interval.Text} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var scriptEl = step.Element("Script");
        var script = scriptEl is not null ? NamedRef.FromXml(scriptEl) : null;
        var intervalEl = step.Element("Interval")?.Element("Calculation");
        var interval = intervalEl is not null ? Calculation.FromXml(intervalEl) : null;
        return new InstallOnTimerScriptStep(script, interval, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        NamedRef? script = null;
        Calculation? interval = null;
        bool scriptSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Interval:", StringComparison.OrdinalIgnoreCase))
            {
                interval = new Calculation(t.Substring(9).Trim());
            }
            else if (!scriptSeen && !string.IsNullOrWhiteSpace(t) && t != "<no script>")
            {
                var name = t;
                if (name.StartsWith("\"") && name.EndsWith("\"") && name.Length >= 2)
                    name = name.Substring(1, name.Length - 2);
                script = new NamedRef(0, name);
                scriptSeen = true;
            }
        }
        return new InstallOnTimerScriptStep(script, interval, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-ontimer-script.html",
        Params =
        [
            new ParamMetadata { Name = "Script", XmlElement = "Script", Type = "script" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Interval" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
