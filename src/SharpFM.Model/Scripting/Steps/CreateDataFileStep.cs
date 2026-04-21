using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CreateDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 190;
    public const string XmlName = "Create Data File";

    public string UniversalPathList { get; set; }
    public bool CreateFolders { get; set; }

    public CreateDataFileStep(
        string universalPathList = "",
        bool createFolders = true,
        bool enabled = true)
        : base(enabled)
    {
        UniversalPathList = universalPathList;
        CreateFolders = createFolders;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", UniversalPathList),
            new XElement("CreateDirectories", new XAttribute("state", CreateFolders ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Create Data File [ " + UniversalPathList + " ; " + "Create folders: " + (CreateFolders ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var universalPathList_v = step.Element("UniversalPathList")?.Value ?? "";
        var createFolders_v = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        return new CreateDataFileStep(universalPathList_v, createFolders_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string universalPathList_v = "";
        bool createFolders_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); createFolders_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        // Unlabeled text param — pick the token that doesn't match known labels.
        universalPathList_v = tokens.FirstOrDefault(t =>
            !t.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase)) ?? "";
        return new CreateDataFileStep(universalPathList_v, createFolders_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/create-data-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
            },
            new ParamMetadata
            {
                Name = "CreateDirectories",
                XmlElement = "CreateDirectories",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Create folders",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
