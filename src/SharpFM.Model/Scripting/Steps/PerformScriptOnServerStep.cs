using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform Script on Server. Script target is either a static reference
/// (<c>&lt;Script&gt;</c> element) or a calculated name
/// (<c>&lt;Calculated&gt;&lt;Calculation/&gt;&lt;/Calculated&gt;</c>).
/// Discriminated via <see cref="PerformScriptTarget"/>.
/// </summary>
public sealed class PerformScriptOnServerStep : ScriptStep, IStepFactory
{
    public const int XmlId = 164;
    public const string XmlName = "Perform Script on Server";

    public bool WaitForCompletion { get; set; }
    public PerformScriptTarget Target { get; set; }
    public Calculation? Parameter { get; set; }

    public PerformScriptOnServerStep(
        bool waitForCompletion = true,
        PerformScriptTarget? target = null,
        Calculation? parameter = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WaitForCompletion = waitForCompletion;
        Target = target ?? new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        Parameter = parameter;
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
                step.Add(new XElement("WaitForCompletion", new XAttribute("state", WaitForCompletion ? "True" : "False")));
                if (Parameter is not null) step.Add(Parameter.ToXml());
                step.Add(byRef.Script.ToXml("Script"));
                break;

            case PerformScriptTarget.ByCalculation byCalc:
                step.Add(new XElement("Calculated", byCalc.NameCalc.ToXml()));
                step.Add(new XElement("WaitForCompletion", new XAttribute("state", WaitForCompletion ? "True" : "False")));
                if (Parameter is not null) step.Add(Parameter.ToXml());
                break;
        }

        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Wait for completion: {(WaitForCompletion ? "On" : "Off")}",
        };
        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                parts.Add(byRef.Script.Id == 0
                    ? $"\"{byRef.Script.Name}\""
                    : $"\"{byRef.Script.Name}\" (#{byRef.Script.Id})");
                break;
            case PerformScriptTarget.ByCalculation byCalc:
                parts.Add($"By name: {byCalc.NameCalc.Text}");
                break;
        }
        if (Parameter is not null) parts.Add($"Parameter: {Parameter.Text}");
        return $"Perform Script on Server [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var wait = step.Element("WaitForCompletion")?.Attribute("state")?.Value == "True";
        var calcEl = step.Element("Calculated")?.Element("Calculation");
        var scriptEl = step.Element("Script");
        PerformScriptTarget target = calcEl is not null
            ? new PerformScriptTarget.ByCalculation(Calculation.FromXml(calcEl))
            : scriptEl is not null
                ? new PerformScriptTarget.ByReference(NamedRef.FromXml(scriptEl))
                : new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        // Parameter is the top-level <Calculation> (not inside Calculated)
        var paramEl = step.Element("Calculation");
        var parameter = paramEl is not null ? Calculation.FromXml(paramEl) : null;
        return new PerformScriptOnServerStep(wait, target, parameter, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool wait = true;
        PerformScriptTarget target = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        Calculation? parameter = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Wait for completion:", System.StringComparison.OrdinalIgnoreCase))
                wait = t.Substring(20).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Parameter:", System.StringComparison.OrdinalIgnoreCase))
                parameter = new Calculation(t.Substring(10).Trim());
            else if (t.StartsWith("By name:", System.StringComparison.OrdinalIgnoreCase))
                target = new PerformScriptTarget.ByCalculation(new Calculation(t.Substring(8).Trim()));
            else if (t.StartsWith("\"") && t.Contains("(#"))
            {
                var idMatch = System.Text.RegularExpressions.Regex.Match(t, @"^""(?<name>.*)""\s*\(#(?<id>\d+)\)$");
                if (idMatch.Success)
                    target = new PerformScriptTarget.ByReference(new NamedRef(int.Parse(idMatch.Groups["id"].Value), idMatch.Groups["name"].Value));
            }
            else if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
                target = new PerformScriptTarget.ByReference(new NamedRef(0, t.Substring(1, t.Length - 2)));
        }
        return new PerformScriptOnServerStep(wait, target, parameter, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-script-on-server.html",
        Params =
        [
            new ParamMetadata { Name = "WaitForCompletion", XmlElement = "WaitForCompletion", XmlAttr = "state", Type = "boolean", HrLabel = "Wait for completion" },
            new ParamMetadata { Name = "Script", XmlElement = "Script", Type = "script" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "Parameter" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
