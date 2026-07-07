using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert PDF inserts a PDF into the active container field. Path uses
/// FileMaker's "image:", "imagemac:", "imagewin:", or "imagelinux:"
/// prefix conventions. When Path is empty, the user is prompted to pick
/// a file. Only works against an interactive container.
/// </summary>
public sealed class InsertPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 158;
    public const string XmlName = "Insert PDF";

    public string Path { get; set; }
    public string StorageType { get; set; }

    private InsertPdfStep() : this("") { }

    public InsertPdfStep(string path = "", string storageType = "Embedded", bool enabled = true)
        : base(enabled)
    {
        Path = path;
        StorageType = storageType;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? XmlName : $"Insert PDF [ {Path} ; {StorageType} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertPdfStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "", type = "Embedded";
        if (hrParams.Length >= 1) path = hrParams[0].Trim();
        if (hrParams.Length >= 2) type = hrParams[1].Trim();
        return new InsertPdfStep(path, type, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-pdf.html",
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
            // The single wire element carries both the path text and the type
            // attribute; these HR-only slots surface them as the two separate
            // display tokens.
            new HrOnly("UniversalPathList"),
            new HrOnly("StorageType") { DisplayValues = ["Embedded", "Reference"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
