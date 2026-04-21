using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class NewWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 122;
    public const string XmlName = "New Window";

    public string LayoutDestination { get; set; }
    public Calculation Calculation { get; set; }
    public Calculation Calculation2 { get; set; }
    public Calculation Calculation3 { get; set; }
    public Calculation Calculation4 { get; set; }
    public Calculation Calculation5 { get; set; }
    public string NewWndStyles { get; set; }

    public NewWindowStep(
        string layoutDestination = "SelectedLayout",
        Calculation? calculation = null,
        Calculation? calculation2 = null,
        Calculation? calculation3 = null,
        Calculation? calculation4 = null,
        Calculation? calculation5 = null,
        string newWndStyles = "",
        bool enabled = true)
        : base(enabled)
    {
        LayoutDestination = layoutDestination;
        Calculation = calculation ?? new Calculation("");
        Calculation2 = calculation2 ?? new Calculation("");
        Calculation3 = calculation3 ?? new Calculation("");
        Calculation4 = calculation4 ?? new Calculation("");
        Calculation5 = calculation5 ?? new Calculation("");
        NewWndStyles = newWndStyles;
    }

    private static readonly IReadOnlyDictionary<string, string> _LayoutDestinationToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["SelectedLayout"] = "SelectedLayout",
    };
    private static readonly IReadOnlyDictionary<string, string> _LayoutDestinationFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["SelectedLayout"] = "SelectedLayout",
    };
    private static string LayoutDestinationHr(string x) => _LayoutDestinationToHr.TryGetValue(x, out var h) ? h : x;
    private static string LayoutDestinationXml(string h) => _LayoutDestinationFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("LayoutDestination", new XAttribute("value", LayoutDestination)),
            new XElement("Name", Calculation.ToXml("Calculation")),
            new XElement("Height", Calculation2.ToXml("Calculation")),
            new XElement("Width", Calculation3.ToXml("Calculation")),
            new XElement("DistanceFromTop", Calculation4.ToXml("Calculation")),
            new XElement("DistanceFromLeft", Calculation5.ToXml("Calculation")),
            new XElement("NewWndStyles", NewWndStyles));

    public override string ToDisplayLine() =>
        "New Window [ " + LayoutDestinationHr(LayoutDestination) + " ; " + Calculation.Text + " ; " + Calculation2.Text + " ; " + Calculation3.Text + " ; " + Calculation4.Text + " ; " + Calculation5.Text + " ; " + NewWndStyles + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var layoutDestination_v = step.Element("LayoutDestination")?.Attribute("value")?.Value ?? "SelectedLayout";
        var calculation_vWrapEl = step.Element("Name");
        var calculation_vCalcEl = calculation_vWrapEl?.Element("Calculation");
        var calculation_v = calculation_vCalcEl is not null ? Calculation.FromXml(calculation_vCalcEl) : new Calculation("");
        var calculation2_vWrapEl = step.Element("Height");
        var calculation2_vCalcEl = calculation2_vWrapEl?.Element("Calculation");
        var calculation2_v = calculation2_vCalcEl is not null ? Calculation.FromXml(calculation2_vCalcEl) : new Calculation("");
        var calculation3_vWrapEl = step.Element("Width");
        var calculation3_vCalcEl = calculation3_vWrapEl?.Element("Calculation");
        var calculation3_v = calculation3_vCalcEl is not null ? Calculation.FromXml(calculation3_vCalcEl) : new Calculation("");
        var calculation4_vWrapEl = step.Element("DistanceFromTop");
        var calculation4_vCalcEl = calculation4_vWrapEl?.Element("Calculation");
        var calculation4_v = calculation4_vCalcEl is not null ? Calculation.FromXml(calculation4_vCalcEl) : new Calculation("");
        var calculation5_vWrapEl = step.Element("DistanceFromLeft");
        var calculation5_vCalcEl = calculation5_vWrapEl?.Element("Calculation");
        var calculation5_v = calculation5_vCalcEl is not null ? Calculation.FromXml(calculation5_vCalcEl) : new Calculation("");
        var newWndStyles_v = step.Element("NewWndStyles")?.Value ?? "";
        return new NewWindowStep(layoutDestination_v, calculation_v, calculation2_v, calculation3_v, calculation4_v, calculation5_v, newWndStyles_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string layoutDestination_v = "SelectedLayout";
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        Calculation? calculation2_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation2_v = new Calculation(tok); break; } }
        Calculation? calculation3_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation3_v = new Calculation(tok); break; } }
        Calculation? calculation4_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation4_v = new Calculation(tok); break; } }
        Calculation? calculation5_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation5_v = new Calculation(tok); break; } }
        string newWndStyles_v = "";
        return new NewWindowStep(layoutDestination_v, calculation_v, calculation2_v, calculation3_v, calculation4_v, calculation5_v, newWndStyles_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/new-window.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "LayoutDestination",
                XmlElement = "LayoutDestination",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["SelectedLayout"],
                DefaultValue = "SelectedLayout",
            },
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
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
            new ParamMetadata
            {
                Name = "NewWndStyles",
                XmlElement = "NewWndStyles",
                Type = "text",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
