using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformQuickFindStep : ScriptStep, IStepFactory
{
    public const int XmlId = 150;
    public const string XmlName = "Perform Quick Find";

    public Calculation Calculation { get; set; }

    public PerformQuickFindStep(
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
        "Perform Quick Find [ " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var calculation_vEl = step.Element("Calculation");
        var calculation_v = calculation_vEl is not null ? Calculation.FromXml(calculation_vEl) : new Calculation("");
        return new PerformQuickFindStep(calculation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        return new PerformQuickFindStep(calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-quick-find.html",
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
