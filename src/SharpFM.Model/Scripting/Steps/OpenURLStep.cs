using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenURLStep : ScriptStep, IStepFactory
{
    public const int XmlId = 111;
    public const string XmlName = "Open URL";

    public bool WithDialog { get; set; }
    public bool InExternalBrowser { get; set; }
    public Calculation Calculation { get; set; }

    public OpenURLStep(
        bool withDialog = true,
        bool inExternalBrowser = false,
        Calculation? calculation = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        InExternalBrowser = inExternalBrowser;
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "True" : "False")),
            new XElement("Option", new XAttribute("state", InExternalBrowser ? "True" : "False")),
            Calculation.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Open URL [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + "In external browser: " + (InExternalBrowser ? "On" : "Off") + " ; " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var inExternalBrowser_v = step.Element("Option")?.Attribute("state")?.Value == "True";
        var calculation_vEl = step.Element("Calculation");
        var calculation_v = calculation_vEl is not null ? Calculation.FromXml(calculation_vEl) : new Calculation("");
        return new OpenURLStep(withDialog_v, inExternalBrowser_v, calculation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool inExternalBrowser_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("In external browser:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); inExternalBrowser_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase) || tok.StartsWith("In external browser:", StringComparison.OrdinalIgnoreCase))) { calculation_v = new Calculation(tok); break; } }
        return new OpenURLStep(withDialog_v, inExternalBrowser_v, calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-url.html",
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
                Name = "Option",
                XmlElement = "Option",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "In external browser",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
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
