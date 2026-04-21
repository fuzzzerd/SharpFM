using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveRecordsAsPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 144;
    public const string XmlName = "Save Records as PDF";

    public bool WithDialog { get; set; }
    public bool Append { get; set; }
    public bool CreateDirectories { get; set; }
    public bool RestoreStoredOptions { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string Path { get; set; }
    public Calculation? StoredLabel { get; set; }
    public PdfOptions? Options { get; set; }

    public SaveRecordsAsPdfStep(
        bool withDialog = false,
        bool append = false,
        bool createDirectories = true,
        bool restoreStoredOptions = true,
        bool autoOpen = false,
        bool createEmail = false,
        string path = "",
        Calculation? storedLabel = null,
        PdfOptions? options = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Append = append;
        CreateDirectories = createDirectories;
        RestoreStoredOptions = restoreStoredOptions;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        StoredLabel = storedLabel;
        Options = options;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("Option", new XAttribute("state", Append ? "True" : "False")),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreStoredOptions ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("UniversalPathList", Path));
        if (StoredLabel is not null) step.Add(StoredLabel.ToXml("Calculation"));
        if (Options is not null) step.Add(Options.ToXml());
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
            Path,
        };
        if (Append) parts.Add("Append");
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        return $"Save Records as PDF [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var append = step.Element("Option")?.Attribute("state")?.Value == "True";
        var createDirs = step.Element("CreateDirectories")?.Attribute("state")?.Value == "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var autoOpen = step.Element("AutoOpen")?.Attribute("state")?.Value == "True";
        var createEmail = step.Element("CreateEmail")?.Attribute("state")?.Value == "True";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var labelEl = step.Element("Calculation");
        var label = labelEl is not null ? Calculation.FromXml(labelEl) : null;
        var optsEl = step.Element("PDFOptions");
        var opts = optsEl is not null ? PdfOptions.FromXml(optsEl) : null;
        return new SaveRecordsAsPdfStep(withDialog, append, createDirs, restore, autoOpen, createEmail, path, label, opts, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Lossy — full PDFOptions can't round-trip through display.
        bool withDialog = false, append = false, autoOpen = false, createEmail = false;
        string path = "";
        bool pathSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", System.StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.Equals("Append", System.StringComparison.OrdinalIgnoreCase)) append = true;
            else if (t.Equals("Automatically open", System.StringComparison.OrdinalIgnoreCase)) autoOpen = true;
            else if (t.Equals("Create email", System.StringComparison.OrdinalIgnoreCase)) createEmail = true;
            else if (!pathSeen && !string.IsNullOrWhiteSpace(t)) { path = t; pathSeen = true; }
        }
        return new SaveRecordsAsPdfStep(withDialog, append, true, true, autoOpen, createEmail, path, null, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-records-as-pdf.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "Option", XmlElement = "Option", XmlAttr = "state", Type = "boolean", HrLabel = "Append" },
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text", Required = true },
            new ParamMetadata { Name = "PDFOptions", XmlElement = "PDFOptions", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
