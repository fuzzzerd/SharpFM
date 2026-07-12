using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseDataFileStep : ScriptStep<CloseDataFileStep>, IStepFactory
{
    public const int XmlId = 196;
    public const string XmlName = "Close Data File";

    public Calculation? FileID { get; set; }

    private CloseDataFileStep() : base(false) { }

    public CloseDataFileStep(
        Calculation? fileID = null,
        bool enabled = true)
        : base(enabled)
    {
        FileID = fileID;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-data-file.html",
        // Canonical unconfigured form is empty: the bare File ID calc is omitted when blank.
        Shape =
        [
            new BareCalcChild { PocoProperty = "FileID", HrLabel = "File ID", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
