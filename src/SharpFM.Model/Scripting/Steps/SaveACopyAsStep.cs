using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 37;
    public const string XmlName = "Save a Copy as";

    public bool CreateFolders { get; set; }
    public bool AutomaticallyOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string CopyType { get; set; } = "Copy";
    public string OutputPath { get; set; } = "";

    private SaveACopyAsStep() : base(false) { }

    public SaveACopyAsStep(
        bool createFolders = true,
        bool automaticallyOpen = false,
        bool createEmail = false,
        string copyType = "Copy",
        string outputPath = "",
        bool enabled = true)
        : base(enabled)
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Save a Copy as [ " + "Create folders: " + (CreateFolders ? "On" : "Off") + " ; " + "Automatically open: " + (AutomaticallyOpen ? "On" : "Off") + " ; " + "Create email: " + (CreateEmail ? "On" : "Off") + " ; " + "Copy type: " + CopyTypeHr(CopyType) + " ; " + "Output path: " + OutputPath + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveACopyAsStep>(step, Metadata);

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
        // Canonical: CreateDirectories, AutoOpen, CreateEmail, SaveAsType, then
        // the optional UniversalPathList (omitted when no output path is set).
        Shape =
        [
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateFolders", HrLabel = "Create folders", Display = DisplayMode.Augmented },
            new BoolStateChild("AutoOpen") { PocoProperty = "AutomaticallyOpen", HrLabel = "Automatically open", Display = DisplayMode.Augmented },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail", HrLabel = "Create email", Display = DisplayMode.Augmented },
            new EnumValueChild("SaveAsType") { PocoProperty = "CopyType", HrLabel = "Copy type", DefaultValue = "Copy", Display = DisplayMode.Augmented },
            new NamedTextChild("UniversalPathList") { PocoProperty = "OutputPath", HrLabel = "Output path", Optional = true, Display = DisplayMode.Augmented },
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
