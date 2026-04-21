using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExitScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 103;
    public const string XmlName = "Exit Script";

    public Calculation Calculation { get; set; }

    public ExitScriptStep(
        Calculation? calculation = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            Calculation.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Exit Script [ " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var calculation_vEl = step.Element("Calculation");
        var calculation_v = calculation_vEl is not null ? Calculation.FromXml(calculation_vEl) : new Calculation("");
        return new ExitScriptStep(calculation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        return new ExitScriptStep(calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/exit-script.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
