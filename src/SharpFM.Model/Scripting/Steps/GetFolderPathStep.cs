using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetFolderPathStep : ScriptStep, IStepFactory
{
    public const int XmlId = 181;
    public const string XmlName = "Get Folder Path";

    public bool AllowFolderCreation { get; set; }
    public string Name { get; set; }
    public Calculation Calculation { get; set; }
    public Calculation Calculation2 { get; set; }
    public Calculation Calculation3 { get; set; }

    public GetFolderPathStep(
        bool allowFolderCreation = true,
        string name = "",
        Calculation? calculation = null,
        Calculation? calculation2 = null,
        Calculation? calculation3 = null,
        bool enabled = true)
        : base(null, enabled)
    {
        AllowFolderCreation = allowFolderCreation;
        Name = name;
        Calculation = calculation ?? new Calculation("");
        Calculation2 = calculation2 ?? new Calculation("");
        Calculation3 = calculation3 ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AllowFolderCreation", new XAttribute("state", AllowFolderCreation ? "True" : "False")),
            new XElement("Name", Name),
            new XElement("DialogTitle", Calculation.ToXml("Calculation")),
            new XElement("DefaultLocation", Calculation2.ToXml("Calculation")),
            new XElement("Repetition", Calculation3.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Get Folder Path [ " + "Allow Folder Creation: " + (AllowFolderCreation ? "On" : "Off") + " ; " + Name + " ; " + Calculation.Text + " ; " + Calculation2.Text + " ; " + Calculation3.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var allowFolderCreation_v = step.Element("AllowFolderCreation")?.Attribute("state")?.Value == "True";
        var name_v = step.Element("Name")?.Value ?? "";
        var calculation_vWrapEl = step.Element("DialogTitle");
        var calculation_vCalcEl = calculation_vWrapEl?.Element("Calculation");
        var calculation_v = calculation_vCalcEl is not null ? Calculation.FromXml(calculation_vCalcEl) : new Calculation("");
        var calculation2_vWrapEl = step.Element("DefaultLocation");
        var calculation2_vCalcEl = calculation2_vWrapEl?.Element("Calculation");
        var calculation2_v = calculation2_vCalcEl is not null ? Calculation.FromXml(calculation2_vCalcEl) : new Calculation("");
        var calculation3_vWrapEl = step.Element("Repetition");
        var calculation3_vCalcEl = calculation3_vWrapEl?.Element("Calculation");
        var calculation3_v = calculation3_vCalcEl is not null ? Calculation.FromXml(calculation3_vCalcEl) : new Calculation("");
        return new GetFolderPathStep(allowFolderCreation_v, name_v, calculation_v, calculation2_v, calculation3_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool allowFolderCreation_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(22).Trim(); allowFolderCreation_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string name_v = "";
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation_v = new Calculation(tok); break; } }
        Calculation? calculation2_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation2_v = new Calculation(tok); break; } }
        Calculation? calculation3_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation3_v = new Calculation(tok); break; } }
        return new GetFolderPathStep(allowFolderCreation_v, name_v, calculation_v, calculation2_v, calculation3_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-directory.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "AllowFolderCreation",
                XmlElement = "AllowFolderCreation",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Allow Folder Creation",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Name",
                XmlElement = "Name",
                Type = "text",
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
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
