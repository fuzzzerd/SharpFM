using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 37;
    public const string XmlName = "Save a Copy as";

    public bool CreateFolders { get; set; }
    public bool AutomaticallyOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string CopyType { get; set; }
    public string OutputPath { get; set; }

    public SaveACopyAsStep(
        bool createFolders = true,
        bool automaticallyOpen = false,
        bool createEmail = false,
        string copyType = "Copy",
        string outputPath = "",
        bool enabled = true)
        : base(null, enabled)
    {
        CreateFolders = createFolders;
        AutomaticallyOpen = automaticallyOpen;
        CreateEmail = createEmail;
        CopyType = copyType;
        OutputPath = outputPath;
    }

    private static readonly IReadOnlyDictionary<string, string> _CopyTypeToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Copy"] = "copy of current file",
        ["CompactedCopy"] = "compacted copy (smaller)",
        ["Clone"] = "clone (no records)",
        ["SelfContainedCopy"] = "self-contained copy (single file)",
    };

    private static readonly IReadOnlyDictionary<string, string> _CopyTypeFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["copy of current file"] = "Copy",
        ["compacted copy (smaller)"] = "CompactedCopy",
        ["clone (no records)"] = "Clone",
        ["self-contained copy (single file)"] = "SelfContainedCopy",
    };

    private static string CopyTypeHr(string x) =>
        _CopyTypeToHr.TryGetValue(x, out var h) ? h : x;

    private static string CopyTypeXml(string h) =>
        _CopyTypeFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("CreateDirectories", new XAttribute("state", CreateFolders ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutomaticallyOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("SaveAsType", new XAttribute("value", CopyType)),
            new XElement("UniversalPathList", OutputPath));

    public override string ToDisplayLine() =>
        "Save a Copy as [ " + "Create folders: " + (CreateFolders ? "On" : "Off") + " ; " + "Automatically open: " + (AutomaticallyOpen ? "On" : "Off") + " ; " + "Create email: " + (CreateEmail ? "On" : "Off") + " ; " + "Copy type: " + CopyTypeHr(CopyType) + " ; " + "Output path: " + OutputPath + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var createFolders_v = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        var automaticallyOpen_v = step.Element("AutoOpen")?.Attribute("state")?.Value == "True";
        var createEmail_v = step.Element("CreateEmail")?.Attribute("state")?.Value == "True";
        var copyType_v = step.Element("SaveAsType")?.Attribute("value")?.Value ?? "Copy";
        var outputPath_v = step.Element("UniversalPathList")?.Value ?? "";
        return new SaveACopyAsStep(createFolders_v, automaticallyOpen_v, createEmail_v, copyType_v, outputPath_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool createFolders_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); createFolders_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool automaticallyOpen_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Automatically open:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(19).Trim(); automaticallyOpen_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool createEmail_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Create email:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); createEmail_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string copyType_v = "Copy";
        foreach (var tok in tokens) { if (tok.StartsWith("Copy type:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(10).Trim(); copyType_v = CopyTypeXml(v); break; } }
        string outputPath_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Output path:", StringComparison.OrdinalIgnoreCase)) { outputPath_v = tok.Substring(12).Trim(); break; } }
        return new SaveACopyAsStep(createFolders_v, automaticallyOpen_v, createEmail_v, copyType_v, outputPath_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-a-copy-as.html",
        Params =
        [
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
            new ParamMetadata
            {
                Name = "AutoOpen",
                XmlElement = "AutoOpen",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Automatically open",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "CreateEmail",
                XmlElement = "CreateEmail",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Create email",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "SaveAsType",
                XmlElement = "SaveAsType",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Copy type",
                ValidValues = ["copy of current file", "compacted copy (smaller)", "clone (no records)", "self-contained copy (single file)"],
                DefaultValue = "Copy",
            },
            new ParamMetadata
            {
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
                HrLabel = "Output path",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
