using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveRecordsAsSnapshotLinkStep : ScriptStep, IStepFactory
{
    public const int XmlId = 152;
    public const string XmlName = "Save Records as Snapshot Link";

    public bool CreateFolders { get; set; }
    public bool CreateEmail { get; set; }
    public string Records { get; set; }
    public string OutputPath { get; set; }

    public SaveRecordsAsSnapshotLinkStep(
        bool createFolders = false,
        bool createEmail = false,
        string records = "BrowsedRecords",
        string outputPath = "",
        bool enabled = true)
        : base(enabled)
    {
        CreateFolders = createFolders;
        CreateEmail = createEmail;
        Records = records;
        OutputPath = outputPath;
    }

    private static readonly IReadOnlyDictionary<string, string> _RecordsToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["BrowsedRecords"] = "Records being browsed",
        ["CurrentRecord"] = "Current record",
    };

    private static readonly IReadOnlyDictionary<string, string> _RecordsFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Records being browsed"] = "BrowsedRecords",
        ["Current record"] = "CurrentRecord",
    };

    private static string RecordsHr(string x) =>
        _RecordsToHr.TryGetValue(x, out var h) ? h : x;

    private static string RecordsXml(string h) =>
        _RecordsFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("CreateDirectories", new XAttribute("state", CreateFolders ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("SaveType", new XAttribute("value", Records)),
            new XElement("UniversalPathList", OutputPath));

    public override string ToDisplayLine() =>
        "Save Records as Snapshot Link [ " + "Create folders: " + (CreateFolders ? "On" : "Off") + " ; " + "Create email: " + (CreateEmail ? "On" : "Off") + " ; " + "Records: " + RecordsHr(Records) + " ; " + "Output path: " + OutputPath + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var createFolders_v = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        var createEmail_v = step.Element("CreateEmail")?.Attribute("state")?.Value == "True";
        var records_v = step.Element("SaveType")?.Attribute("value")?.Value ?? "BrowsedRecords";
        var outputPath_v = step.Element("UniversalPathList")?.Value ?? "";
        return new SaveRecordsAsSnapshotLinkStep(createFolders_v, createEmail_v, records_v, outputPath_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool createFolders_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); createFolders_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool createEmail_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Create email:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); createEmail_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string records_v = "BrowsedRecords";
        foreach (var tok in tokens) { if (tok.StartsWith("Records:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(8).Trim(); records_v = RecordsXml(v); break; } }
        string outputPath_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Output path:", StringComparison.OrdinalIgnoreCase)) { outputPath_v = tok.Substring(12).Trim(); break; } }
        return new SaveRecordsAsSnapshotLinkStep(createFolders_v, createEmail_v, records_v, outputPath_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-records-as-snapshot-link.html",
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
                Name = "SaveType",
                XmlElement = "SaveType",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Records",
                ValidValues = ["Records being browsed", "Current record"],
                DefaultValue = "BrowsedRecords",
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
