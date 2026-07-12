using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Picture inserts an image into the active container field.
/// Same shape as Insert PDF — a UniversalPathList with Embedded/Reference
/// storage type.
/// </summary>
public sealed class InsertPictureStep : ScriptStep<InsertPictureStep>, IStepFactory
{
    public const int XmlId = 56;
    public const string XmlName = "Insert Picture";

    public string Path { get; set; }
    public string StorageType { get; set; }

    private InsertPictureStep() : this("") { }

    public InsertPictureStep(string path = "", string storageType = "Embedded", bool enabled = true)
        : base(enabled)
    {
        Path = path;
        StorageType = storageType;
    }

    // Hand-written: the single wire element splits into two display tokens
    // (path text + type attribute) via HrOnly slots the renderer cannot drive.
    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? XmlName : $"Insert Picture [ {Path} ; {StorageType} ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        string path = "", type = "Embedded";
        if (hrParams.Length >= 1) path = hrParams[0].Trim();
        if (hrParams.Length >= 2) type = hrParams[1].Trim();
        Path = path;
        StorageType = type;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-picture.html",
        Shape =
        [
            new NamedTextChild("UniversalPathList")
            {
                PocoProperty = "Path",
                Attr = "type",
                AttrProperty = "StorageType",
                AttrDefault = "Embedded",
                ValidValues = ["Embedded", "Reference"],
                Display = DisplayMode.Hidden,
            },
            new HrOnly("UniversalPathList"),
            new HrOnly("StorageType") { DisplayValues = ["Embedded", "Reference"] },
        ],
    };
}
