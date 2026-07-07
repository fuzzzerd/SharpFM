using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Picture inserts an image into the active container field.
/// Same shape as Insert PDF — a UniversalPathList with Embedded/Reference
/// storage type.
/// </summary>
public sealed class InsertPictureStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? XmlName : $"Insert Picture [ {Path} ; {StorageType} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertPictureStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "", type = "Embedded";
        if (hrParams.Length >= 1) path = hrParams[0].Trim();
        if (hrParams.Length >= 2) type = hrParams[1].Trim();
        return new InsertPictureStep(path, type, enabled);
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
            },
        ],
        Params =
        [
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "StorageType", XmlElement = "UniversalPathList", XmlAttr = "type", Type = "enum", ValidValues = ["Embedded", "Reference"], DefaultValue = "Embedded" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
