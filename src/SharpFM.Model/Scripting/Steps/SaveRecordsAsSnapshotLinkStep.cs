using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveRecordsAsSnapshotLinkStep : ScriptStep<SaveRecordsAsSnapshotLinkStep>, IStepFactory
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
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateFolders", HrLabel = "Create folders", Display = DisplayMode.Native },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail", HrLabel = "Create email", Display = DisplayMode.Native },
            new EnumValueChild("SaveType") { PocoProperty = "Records", HrLabel = "Records", DefaultValue = "BrowsedRecords", ValidValues = ["BrowsedRecords", "CurrentRecord"], DisplayValues = ["Records being browsed", "Current record"], Display = DisplayMode.Native },
            new NamedTextChild("UniversalPathList") { PocoProperty = "OutputPath", HrLabel = "Output path", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
