using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 34;
    public const string XmlName = "Close File";

    public FileReference? File { get; set; }

    public CloseFileStep(FileReference? file = null, bool enabled = true)
        : base(enabled)
    {
        File = file;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (File is not null) step.Add(File.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        File is null
            ? "Close File [ Current File ]"
            : $"Close File [ {File.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fileEl = step.Element("FileReference");
        var file = fileEl is not null ? FileReference.FromXml(fileEl) : null;
        return new CloseFileStep(file, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FileReference? file = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Current File", System.StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(t)) continue;
            file = FileReference.FromDisplayToken(t);
            break;
        }
        return new CloseFileStep(file, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "FileReference",
                XmlElement = "FileReference",
                Type = "fileReference",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
