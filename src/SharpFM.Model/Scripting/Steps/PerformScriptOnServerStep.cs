using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    // Emit-only wire projections: FM Pro emits <Calculated> before
    // <WaitForCompletion> for ByCalculation but <Script> after the parameter
    // for ByReference. Get-only, so the shape parser skips them.
    public Calculation? CalculatedWire =>
        Target is PerformScriptTarget.ByCalculation byCalc ? byCalc.NameCalc : null;
    public NamedRef? ScriptWire =>
        Target is PerformScriptTarget.ByReference byRef ? byRef.Script : null;

    public PerformScriptOnServerStep(
        bool waitForCompletion = true,
        PerformScriptTarget? target = null,
        Calculation? parameter = null,
        bool enabled = true)
        : base(enabled)
    {
        WaitForCompletion = waitForCompletion;
        Target = target ?? new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        Parameter = parameter;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: the script target is a variant with a bare quoted-name token order the shape renderer cannot express.
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

    // Hand-written rather than StepXmlParser: the Target union is spread
    // across two emit-only projection slots, and the degenerate no-reference
    // form defaults to an empty by-ref.
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
        Shape =
        [
            // Per-variant ordering via the emit-only projections above:
            // <Calculated> leads for ByCalculation, <Script> trails the
            // parameter for ByReference; WaitForCompletion and the parameter
            // sit between in both variants.
            new NamedCalcChild("Calculated") { PocoProperty = "CalculatedWire", Optional = true },
            new BoolStateChild("WaitForCompletion") { HrLabel = "Wait for completion" },
            new BareCalcChild { PocoProperty = "Parameter", Optional = true, HrLabel = "Parameter" },
            new NamedRefChild("Script") { PocoProperty = "ScriptWire", Optional = true, HrLabel = "Script" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
