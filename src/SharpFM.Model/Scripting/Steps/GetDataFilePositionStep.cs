using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetDataFilePositionStep : ScriptStep<GetDataFilePositionStep>, IStepFactory
{
    public const int XmlId = 194;
    public const string XmlName = "Get Data File Position";

    public Calculation? FileId { get; set; }
    public FieldRef? Target { get; set; }

    private GetDataFilePositionStep() : base(false) { }

    public GetDataFilePositionStep(Calculation? fileId = null, FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        FileId = fileId;
        Target = target;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-data-file-position.html",
        // Canonical unconfigured form is empty: the bare File ID calc and target are omitted when absent.
        Shape =
        [
            new BareCalcChild { PocoProperty = "FileId", HrLabel = "File ID", Required = true, Optional = true, DisplayEmptyAs = "" },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
    };
}
