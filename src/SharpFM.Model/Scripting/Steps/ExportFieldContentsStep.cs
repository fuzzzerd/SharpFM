using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExportFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 132;
    public const string XmlName = "Export Field Contents";

    public FieldRef? Field { get; set; }
    public string Path { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public bool CreateDirectories { get; set; }

    public ExportFieldContentsStep(
        FieldRef? field = null,
        string path = "",
        bool autoOpen = false,
        bool createEmail = false,
        bool createDirectories = true,
        bool enabled = true)
        : base(null, enabled)
    {
        Field = field;
        Path = path;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        CreateDirectories = createDirectories;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("UniversalPathList", Path));
        if (Field is not null) step.Add(Field.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Field is not null) parts.Add(Field.ToDisplayString());
        parts.Add(Path);
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        parts.Add($"Create folders: {(CreateDirectories ? "On" : "Off")}");
        return $"Export Field Contents [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var createDirs = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        var autoOpen = step.Element("AutoOpen")?.Attribute("state")?.Value == "True";
        var createEmail = step.Element("CreateEmail")?.Attribute("state")?.Value == "True";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new ExportFieldContentsStep(field, path, autoOpen, createEmail, createDirs, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FieldRef? field = null;
        string path = "";
        bool autoOpen = false;
        bool createEmail = false;
        bool createDirs = true;
        int positional = 0;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Automatically open", StringComparison.OrdinalIgnoreCase))
                autoOpen = true;
            else if (t.Equals("Create email", StringComparison.OrdinalIgnoreCase))
                createEmail = true;
            else if (t.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase))
                createDirs = t.Substring(15).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!string.IsNullOrWhiteSpace(t))
            {
                if (positional == 0) field = FieldRef.FromDisplayToken(t);
                else if (positional == 1) path = t;
                positional++;
            }
        }
        return new ExportFieldContentsStep(field, path, autoOpen, createEmail, createDirs, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/export-field-contents.html",
        Params =
        [
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean", HrLabel = "Automatically open" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean", HrLabel = "Create email" },
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean", HrLabel = "Create folders", ValidValues = ["On", "Off"], DefaultValue = "True" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
