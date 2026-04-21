using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 33;
    public const string XmlName = "Open File";

    public bool OpenHidden { get; set; }
    public FileReference? File { get; set; }

    public OpenFileStep(bool openHidden = false, FileReference? file = null, bool enabled = true)
        : base(enabled)
    {
        OpenHidden = openHidden;
        File = file;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", OpenHidden ? "True" : "False")));
        if (File is not null) step.Add(File.ToXml());
        return step;
    }

    public override string ToDisplayLine()
    {
        var hidden = "Open hidden: " + (OpenHidden ? "On" : "Off");
        return File is null
            ? $"Open File [ {hidden} ]"
            : $"Open File [ {hidden} ; {File.ToDisplayString()} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var hidden = step.Element("Option")?.Attribute("state")?.Value == "True";
        var fileEl = step.Element("FileReference");
        var file = fileEl is not null ? FileReference.FromXml(fileEl) : null;
        return new OpenFileStep(hidden, file, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool hidden = false;
        FileReference? file = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Open hidden:", StringComparison.OrdinalIgnoreCase))
            {
                hidden = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                file = FileReference.FromDisplayToken(t);
            }
        }
        return new OpenFileStep(hidden, file, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                XmlAttr = "state",
                Type = "boolean",
                HrLabel = "Open hidden",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "FileReference",
                XmlElement = "FileReference",
                Type = "fileReference",
            },
        ],
        Notes = new StepNotes
        {
            Behavioral = "Opens a FileMaker file or reestablishes the link to an ODBC data source. Script steps after Open File execute in the file containing the script, not the opened file.",
            Constraints = "Cannot open a file from an unauthorized (unlinked) file.",
            Platform = new StepPlatformNotes
            {
                WebDirect = "Not supported.",
                Server = "Not supported.",
            },
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
