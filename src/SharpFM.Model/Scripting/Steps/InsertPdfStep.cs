using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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

    public InsertPdfStep(string path = "", string storageType = "Embedded", bool enabled = true)
        : base(null, enabled)
    {
        Path = path;
        StorageType = storageType;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", new XAttribute("type", StorageType), Path));

    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? XmlName : $"Insert PDF [ {Path} ; {StorageType} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var pathEl = step.Element("UniversalPathList");
        return new InsertPdfStep(
            pathEl?.Value ?? "",
            pathEl?.Attribute("type")?.Value ?? "Embedded",
            enabled);
    }

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
        Params =
        [
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "StorageType", XmlElement = "UniversalPathList", XmlAttr = "type", Type = "enum", ValidValues = ["Embedded", "Reference"], DefaultValue = "Embedded" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
