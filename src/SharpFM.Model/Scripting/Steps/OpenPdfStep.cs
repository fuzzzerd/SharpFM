using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Open PDF starts a multi-step PDF from an existing file. The
/// <c>&lt;Option&gt;</c> toggle pairs with the optional
/// <c>&lt;UniversalPathList&gt;</c>, and the <c>&lt;OpenPDFFile&gt;</c>
/// wrapper carries the save type plus an optional open-password calculation.
/// </summary>
public sealed class OpenPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 246;
    public const string XmlName = "Open PDF";

    public bool SpecifyFile { get; set; }
    public string Path { get; set; }
    public string SaveType { get; set; }
    public Calculation? OpenPassword { get; set; }

    public OpenPdfStep(
        bool specifyFile = false,
        string path = "",
        string saveType = "File",
        Calculation? openPassword = null,
        bool enabled = true)
        : base(enabled)
    {
        SpecifyFile = specifyFile;
        Path = path;
        SaveType = saveType;
        OpenPassword = openPassword;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", SpecifyFile ? "True" : "False")));
        if (!string.IsNullOrEmpty(Path)) step.Add(new XElement("UniversalPathList", Path));
        var file = new XElement("OpenPDFFile", new XElement("PDFSaveType", SaveType));
        if (OpenPassword is not null) file.Add(new XElement("OpenPassword", OpenPassword.ToXml("Calculation")));
        step.Add(file);
        return step;
    }

    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? "Open PDF" : $"Open PDF [ {Path} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var specifyFile = step.Element("Option")?.Attribute("state")?.Value == "True";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var file = step.Element("OpenPDFFile");
        var saveType = file?.Element("PDFSaveType")?.Value ?? "File";
        var pwdEl = file?.Element("OpenPassword")?.Element("Calculation");
        var pwd = pwdEl is not null ? Calculation.FromXml(pwdEl) : null;
        return new OpenPdfStep(specifyFile, path, saveType, pwd, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "";
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!string.IsNullOrWhiteSpace(t)) { path = t; break; }
        }
        return new OpenPdfStep(!string.IsNullOrEmpty(path), path, "File", null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-pdf.html",
        Params =
        [
            new ParamMetadata { Name = "Option", XmlElement = "Option", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "OpenPDFFile", XmlElement = "OpenPDFFile", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
