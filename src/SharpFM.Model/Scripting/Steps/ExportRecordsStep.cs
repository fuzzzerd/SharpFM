using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// Shape-facing view of the complex children no primitive models. The
    /// getter emits only <c>&lt;Profile&gt;</c> (this passthrough slot sits
    /// before <c>&lt;UniversalPathList&gt;</c>); the setter receives every
    /// unmodeled child — the parser populates only the first passthrough slot —
    /// and demultiplexes Profile, ExportOptions, ExportEntries and
    /// SummaryFields. <see cref="TrailingWire"/> re-emits the latter three
    /// after the path.
    /// </summary>
    public List<XElement> ProfileWire
    {
        get => Profile is null ? [] : [AttrElement("Profile", Profile)];
        set
        {
            static Dictionary<string, string>? AttrsOf(XElement? el) =>
                el?.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
            Profile = AttrsOf(value.FirstOrDefault(e => e.Name.LocalName == "Profile"));
            ExportOptions = AttrsOf(value.FirstOrDefault(e => e.Name.LocalName == "ExportOptions"));
            ExportEntries = value.FirstOrDefault(e => e.Name.LocalName == "ExportEntries")
                ?.Elements("ExportEntry").Select(ExportEntry.FromXml).ToList() ?? new List<ExportEntry>();
            SummaryFields = value.FirstOrDefault(e => e.Name.LocalName == "SummaryFields")
                ?.Elements("Field").Select(SummaryFieldEntry.FromXml).ToList() ?? new List<SummaryFieldEntry>();
        }
    }

    /// <summary>Emit-only wire view of the post-path complex children. Get-only, so the shape parser skips it.</summary>
    public List<XElement> TrailingWire
    {
        get
        {
            var list = new List<XElement>();
            if (ExportOptions is not null) list.Add(AttrElement("ExportOptions", ExportOptions));
            if (ExportEntries.Count > 0) list.Add(new XElement("ExportEntries", ExportEntries.Select(e => e.ToXml())));
            if (SummaryFields.Count > 0) list.Add(new XElement("SummaryFields", SummaryFields.Select(s => s.ToXml())));
            return list;
        }
    }

    private static XElement AttrElement(string name, IReadOnlyDictionary<string, string> attrs)
    {
        var el = new XElement(name);
        foreach (var kv in attrs) el.Add(new XAttribute(kv.Key, kv.Value));
        return el;
    }

    private ExportRecordsStep() : this(enabled: true) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Export Records [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExportRecordsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ExportRecordsStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/export-records.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("CreateDirectories"),
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOrder" },
            new BoolStateChild("AutoOpen"),
            new BoolStateChild("CreateEmail"),
            // <Profile> precedes the path; the parse side of this slot also
            // absorbs the trailing complex children (see ProfileWire).
            new Passthrough { PocoProperty = "ProfileWire" },
            new HrOnly("Profile"),
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new Passthrough { PocoProperty = "TrailingWire" },
            new HrOnly("ExportOptions"),
            new HrOnly("ExportEntries"),
            new HrOnly("SummaryFields"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
