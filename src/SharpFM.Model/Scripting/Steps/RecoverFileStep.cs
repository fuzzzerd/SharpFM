using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RecoverFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 95;
    public const string XmlName = "Recover File";

    public bool WithDialog { get; set; }
    public string UniversalPathList { get; set; }

    public RecoverFileStep(
        bool withDialog = true,
        string universalPathList = "",
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        UniversalPathList = universalPathList;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("UniversalPathList", UniversalPathList));

    public override string ToDisplayLine() =>
        "Recover File [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + UniversalPathList + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var universalPathList_v = step.Element("UniversalPathList")?.Value ?? "";
        return new RecoverFileStep(withDialog_v, universalPathList_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        var universalPathList_v = tokens.FirstOrDefault(t =>
            !t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) ?? "";
        return new RecoverFileStep(withDialog_v, universalPathList_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/recover-file.html",
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
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
