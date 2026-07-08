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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveACopyAsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SaveACopyAsStep>(enabled, hrParams, Metadata);

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
            // All Native: every token always shows (Augmented would suppress the
            // Copy type at its DefaultValue), and shape order is the display order.
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateFolders", HrLabel = "Create folders", Display = DisplayMode.Native },
            new BoolStateChild("AutoOpen") { PocoProperty = "AutomaticallyOpen", HrLabel = "Automatically open", Display = DisplayMode.Native },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail", HrLabel = "Create email", Display = DisplayMode.Native },
            new EnumValueChild("SaveAsType") { PocoProperty = "CopyType", HrLabel = "Copy type", DefaultValue = "Copy", ValidValues = ["Copy", "CompactedCopy", "Clone", "SelfContainedCopy"], DisplayValues = ["copy of current file", "compacted copy (smaller)", "clone (no records)", "self-contained copy (single file)"], Display = DisplayMode.Native },
            new NamedTextChild("UniversalPathList") { PocoProperty = "OutputPath", HrLabel = "Output path", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
