using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenDataFileStep : ScriptStep<OpenDataFileStep>, IStepFactory
{
    public const int XmlId = 191;
    public const string XmlName = "Open Data File";

    public string Path { get; set; }
    public FieldRef? Target { get; set; }

    private OpenDataFileStep() : base(false) { Path = ""; }

    public OpenDataFileStep(string path = "", FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Target = target;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-data-file.html",
        // Canonical unconfigured form is empty: path and target are omitted when absent.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Required = true, Optional = true, DisplayEmptyAs = "" },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
    };
}
