using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OmitMultipleRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 26;
    public const string XmlName = "Omit Multiple Records";

    public bool WithDialog { get; set; }
    public Calculation Calculation { get; set; }

    public OmitMultipleRecordsStep(
        bool withDialog = true,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "True" : "False")),
            Calculation.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Omit Multiple Records [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var calculation_vEl = step.Element("Calculation");
        var calculation_v = calculation_vEl is not null ? Calculation.FromXml(calculation_vEl) : new Calculation("");
        return new OmitMultipleRecordsStep(withDialog_v, calculation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))) { calculation_v = new Calculation(tok); break; } }
        return new OmitMultipleRecordsStep(withDialog_v, calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/omit-multiple-records.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
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
