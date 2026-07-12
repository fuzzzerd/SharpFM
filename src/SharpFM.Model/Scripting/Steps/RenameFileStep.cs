using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RenameFileStep : ScriptStep<RenameFileStep>, IStepFactory
{
    public const int XmlId = 199;
    public const string XmlName = "Rename File";

    public string SourceFile { get; set; }
    public Calculation? NewName { get; set; }

    private RenameFileStep() : base(false) { SourceFile = ""; }

    public RenameFileStep(
        string sourceFile = "",
        Calculation? newName = null,
        bool enabled = true)
        : base(enabled)
    {
        SourceFile = sourceFile;
        NewName = newName;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/rename-file.html",
        // Canonical unconfigured form is empty: source path and new-name calc are omitted when blank.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "SourceFile", HrLabel = "Source file", Optional = true, DisplayEmptyAs = "" },
            new BareCalcChild { PocoProperty = "NewName", HrLabel = "New name", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
