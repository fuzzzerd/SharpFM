using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveRecordsAsSnapshotLinkStep : ScriptStep, IStepFactory
{
    public const int XmlId = 152;
    public const string XmlName = "Save Records as Snapshot Link";

    public bool CreateFolders { get; set; }
    public bool CreateEmail { get; set; }
    public string Records { get; set; }
    public string OutputPath { get; set; }

    private SaveRecordsAsSnapshotLinkStep() : base(false) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Save Records as Snapshot Link [ " + "Create folders: " + (CreateFolders ? "On" : "Off") + " ; " + "Create email: " + (CreateEmail ? "On" : "Off") + " ; " + "Records: " + RecordsHr(Records) + " ; " + "Output path: " + OutputPath + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveRecordsAsSnapshotLinkStep>(step, Metadata);

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
        // Canonical: CreateDirectories, CreateEmail, SaveType, then the path list
        // which the unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateFolders", HrLabel = "Create folders", Display = DisplayMode.Hidden },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail", HrLabel = "Create email", Display = DisplayMode.Hidden },
            new EnumValueChild("SaveType") { PocoProperty = "Records", HrLabel = "Records", DefaultValue = "BrowsedRecords", Display = DisplayMode.Hidden },
            new NamedTextChild("UniversalPathList") { PocoProperty = "OutputPath", HrLabel = "Output path", Optional = true, Display = DisplayMode.Native },
        ],
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
