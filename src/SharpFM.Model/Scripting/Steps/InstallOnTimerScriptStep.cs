using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    private InstallOnTimerScriptStep() : base(false) { }

    public InstallOnTimerScriptStep(NamedRef? script = null, Calculation? interval = null, bool enabled = true)
        : base(enabled)
    {
        Script = script;
        Interval = interval;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var script = Script is null ? "<no script>" : $"\"{Script.Name}\"";
        return Interval is null
            ? $"Install OnTimer Script [ {script} ]"
            : $"Install OnTimer Script [ {script} ; Interval: {Interval.Text} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InstallOnTimerScriptStep>(step, Metadata);

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
        // Canonical §8.1: element order Interval -> Script (the §7.3 trap is the
        // interval being wrapped in <Interval>, which NamedCalcChild guarantees).
        // Both are optional — the cancel-all form has neither child.
        Shape =
        [
            new NamedCalcChild("Interval") { PocoProperty = "Interval", HrLabel = "Interval", Optional = true, Display = DisplayMode.Augmented },
            new NamedRefChild("Script") { PocoProperty = "Script", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata { Name = "Script", XmlElement = "Script", Type = "script" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Interval" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
