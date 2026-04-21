using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Export Records. Profile holds the export format config (DataType,
/// delimiter, header row options). ExportEntries is the ordered list of
/// fields to export; SummaryFields is an optional list of summary fields
/// that drives grouping.
/// </summary>
public sealed class ExportRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 36;
    public const string XmlName = "Export Records";

    public bool WithDialog { get; set; }
    public bool CreateDirectories { get; set; }
    public bool RestoreStoredOrder { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public IReadOnlyDictionary<string, string>? Profile { get; set; }
    public string Path { get; set; }
    public IReadOnlyDictionary<string, string>? ExportOptions { get; set; }
    public IReadOnlyList<ExportEntry> ExportEntries { get; set; }
    public IReadOnlyList<SummaryFieldEntry> SummaryFields { get; set; }

    public ExportRecordsStep(
        bool withDialog = false, bool createDirectories = true, bool restoreStoredOrder = true,
        bool autoOpen = true, bool createEmail = true,
        IReadOnlyDictionary<string, string>? profile = null,
        string path = "",
        IReadOnlyDictionary<string, string>? exportOptions = null,
        IReadOnlyList<ExportEntry>? exportEntries = null,
        IReadOnlyList<SummaryFieldEntry>? summaryFields = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        CreateDirectories = createDirectories;
        RestoreStoredOrder = restoreStoredOrder;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Profile = profile;
        Path = path;
        ExportOptions = exportOptions;
        ExportEntries = exportEntries ?? new List<ExportEntry>();
        SummaryFields = summaryFields ?? new List<SummaryFieldEntry>();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreStoredOrder ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")));
        if (Profile is not null)
        {
            var prof = new XElement("Profile");
            foreach (var kv in Profile) prof.Add(new XAttribute(kv.Key, kv.Value));
            step.Add(prof);
        }
        step.Add(new XElement("UniversalPathList", Path));
        if (ExportOptions is not null)
        {
            var opts = new XElement("ExportOptions");
            foreach (var kv in ExportOptions) opts.Add(new XAttribute(kv.Key, kv.Value));
            step.Add(opts);
        }
        if (ExportEntries.Count > 0)
        {
            var entries = new XElement("ExportEntries");
            foreach (var e in ExportEntries) entries.Add(e.ToXml());
            step.Add(entries);
        }
        if (SummaryFields.Count > 0)
        {
            var summaries = new XElement("SummaryFields");
            foreach (var s in SummaryFields) summaries.Add(s.ToXml());
            step.Add(summaries);
        }
        return step;
    }

    public override string ToDisplayLine() =>
        $"Export Records [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        static Dictionary<string, string>? AttrsOf(XElement? el) =>
            el is null ? null : el.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);

        return new ExportRecordsStep(
            step.Element("NoInteract")?.Attribute("state")?.Value != "True",
            step.Element("CreateDirectories")?.Attribute("state")?.Value == "True",
            step.Element("Restore")?.Attribute("state")?.Value == "True",
            step.Element("AutoOpen")?.Attribute("state")?.Value == "True",
            step.Element("CreateEmail")?.Attribute("state")?.Value == "True",
            AttrsOf(step.Element("Profile")),
            step.Element("UniversalPathList")?.Value ?? "",
            AttrsOf(step.Element("ExportOptions")),
            step.Element("ExportEntries")?.Elements("ExportEntry").Select(ExportEntry.FromXml).ToList(),
            step.Element("SummaryFields")?.Elements("Field").Select(SummaryFieldEntry.FromXml).ToList(),
            enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ExportRecordsStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/export-records.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Profile", XmlElement = "Profile", Type = "complex" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "ExportOptions", XmlElement = "ExportOptions", Type = "complex" },
            new ParamMetadata { Name = "ExportEntries", XmlElement = "ExportEntries", Type = "complex" },
            new ParamMetadata { Name = "SummaryFields", XmlElement = "SummaryFields", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
