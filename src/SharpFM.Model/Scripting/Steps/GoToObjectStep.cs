using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToObjectStep : ScriptStep, IStepFactory
{
    public const int XmlId = 145;
    public const string XmlName = "Go to Object";

    public Calculation Calculation { get; set; }
    public Calculation Calculation2 { get; set; }

    public GoToObjectStep(
        Calculation? calculation = null,
        Calculation? calculation2 = null,
        bool enabled = true)
        : base(enabled)
    {
        Calculation = calculation ?? new Calculation("");
        Calculation2 = calculation2 ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("ObjectName", Calculation.ToXml("Calculation")),
            new XElement("Repetition", Calculation2.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Go to Object [ " + Calculation.Text + " ; " + Calculation2.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var calculation_vWrapEl = step.Element("ObjectName");
        var calculation_vCalcEl = calculation_vWrapEl?.Element("Calculation");
        var calculation_v = calculation_vCalcEl is not null ? Calculation.FromXml(calculation_vCalcEl) : new Calculation("");
        var calculation2_vWrapEl = step.Element("Repetition");
        var calculation2_vCalcEl = calculation2_vWrapEl?.Element("Calculation");
        var calculation2_v = calculation2_vCalcEl is not null ? Calculation.FromXml(calculation2_vCalcEl) : new Calculation("");
        return new GoToObjectStep(calculation_v, calculation2_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        Calculation? calculation2_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation2_v = new Calculation(tok); break; } }
        return new GoToObjectStep(calculation_v, calculation2_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-object.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
