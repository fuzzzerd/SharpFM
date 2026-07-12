using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CreateDataFileStep : ScriptStep<CreateDataFileStep>, IStepFactory
{
    public const int XmlId = 190;
    public const string XmlName = "Create Data File";

    public string UniversalPathList { get; set; }
    public bool CreateFolders { get; set; }

    private CreateDataFileStep() : base(false) { UniversalPathList = ""; CreateFolders = true; }

    public CreateDataFileStep(
        string universalPathList = "",
        bool createFolders = true,
        bool enabled = true)
        : base(enabled)
    {
        UniversalPathList = universalPathList;
        CreateFolders = createFolders;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/create-data-file.html",
        // Canonical form omits the (optional) path; CreateDirectories is always emitted.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "UniversalPathList", Optional = true, DisplayEmptyAs = "" },
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateFolders", HrLabel = "Create folders" },
        ],
    };
}
