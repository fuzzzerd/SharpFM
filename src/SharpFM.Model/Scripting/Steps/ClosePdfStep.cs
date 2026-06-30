using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Close PDF finalises a multi-step PDF and carries the post-close behaviour
/// flags (<c>CreateDirectories</c> / <c>AutoOpen</c> / <c>CreateEmail</c>),
/// an optional output path, and a <c>&lt;ClosePDFFile&gt;</c> wrapper holding
/// the save type.
/// </summary>
public sealed class ClosePdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 245;
    public const string XmlName = "Close PDF";

    public bool CreateDirectories { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string Path { get; set; }
    public string SaveType { get; set; }

    public ClosePdfStep(
        bool createDirectories = false,
        bool autoOpen = false,
        bool createEmail = false,
        string path = "",
        string saveType = "File",
        bool enabled = true)
        : base(enabled)
    {
        CreateDirectories = createDirectories;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        SaveType = saveType;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")));
        if (!string.IsNullOrEmpty(Path)) step.Add(new XElement("UniversalPathList", Path));
        step.Add(new XElement("ClosePDFFile", new XElement("PDFSaveType", SaveType)));
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Path)) parts.Add(Path);
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        return parts.Count == 0
            ? "Close PDF"
            : $"Close PDF [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var createDirs = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        var autoOpen = step.Element("AutoOpen")?.Attribute("state")?.Value == "True";
        var createEmail = step.Element("CreateEmail")?.Attribute("state")?.Value == "True";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var saveType = step.Element("ClosePDFFile")?.Element("PDFSaveType")?.Value ?? "File";
        return new ClosePdfStep(createDirs, autoOpen, createEmail, path, saveType, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool autoOpen = false, createEmail = false;
        string path = "";
        bool pathSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Automatically open", System.StringComparison.OrdinalIgnoreCase)) autoOpen = true;
            else if (t.Equals("Create email", System.StringComparison.OrdinalIgnoreCase)) createEmail = true;
            else if (!pathSeen && !string.IsNullOrWhiteSpace(t)) { path = t; pathSeen = true; }
        }
        return new ClosePdfStep(false, autoOpen, createEmail, path, "File", enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-pdf.html",
        Params =
        [
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "ClosePDFFile", XmlElement = "ClosePDFFile", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
