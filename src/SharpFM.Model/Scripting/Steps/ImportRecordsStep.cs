using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Import Records. Profile attributes vary by DataSourceType (File, Folder,
/// XMLSource); we preserve them as opaque attribute dictionaries. Field
/// mapping is typed via ImportTargetField (map: Import / DoNotImport /
/// Match).
/// </summary>
public sealed class ImportRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 35;
    public const string XmlName = "Import Records";

    public bool WithDialog { get; set; }
    public bool RestoreStoredOrder { get; set; }
    public bool VerifySslCertificates { get; set; }
    public string DataSourceType { get; set; }
    public string Path { get; set; }
    public IReadOnlyDictionary<string, string>? ImportOptions { get; set; }
    public NamedRef? Table { get; set; }
    public IReadOnlyList<ImportTargetField> TargetFields { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// <c>&lt;DataSourceType&gt;</c> wire projection: the descriptor is emitted
    /// only when a source path is configured (null suppresses the element).
    /// </summary>
    public string? DataSourceTypeWire
    {
        get => string.IsNullOrEmpty(Path) ? null : DataSourceType;
        set => DataSourceType = string.IsNullOrEmpty(value) ? "File" : value;
    }

    /// <summary>
    /// Shape-facing view of the trailing complex children no primitive models
    /// (attribute-dictionary <c>&lt;ImportOptions&gt;</c>, the target
    /// <c>&lt;Table&gt;</c> reference and the <c>&lt;TargetFields&gt;</c> map),
    /// emitted and parsed back through the shape's passthrough slot.
    /// </summary>
    public List<XElement> ComplexWire
    {
        get
        {
            var list = new List<XElement>();
            if (ImportOptions is not null)
            {
                var opts = new XElement("ImportOptions");
                foreach (var kv in ImportOptions) opts.Add(new XAttribute(kv.Key, kv.Value));
                list.Add(opts);
            }
            if (Table is not null) list.Add(Table.ToXml("Table"));
            if (TargetFields.Count > 0)
                list.Add(new XElement("TargetFields", TargetFields.Select(f => f.ToXml())));
            return list;
        }
        set
        {
            ImportOptions = value.FirstOrDefault(e => e.Name.LocalName == "ImportOptions")
                ?.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
            var tableEl = value.FirstOrDefault(e => e.Name.LocalName == "Table");
            Table = tableEl is not null ? NamedRef.FromXml(tableEl) : null;
            TargetFields = value.FirstOrDefault(e => e.Name.LocalName == "TargetFields")
                ?.Elements("Field").Select(ImportTargetField.FromXml).ToList() ?? new List<ImportTargetField>();
        }
    }

    private ImportRecordsStep() : this(enabled: true) { }

    public ImportRecordsStep(
        bool withDialog = false, bool restoreStoredOrder = true, bool verifySslCertificates = false,
        string dataSourceType = "File", string path = "",
        IReadOnlyDictionary<string, string>? importOptions = null,
        NamedRef? table = null,
        IReadOnlyList<ImportTargetField>? targetFields = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        RestoreStoredOrder = restoreStoredOrder;
        VerifySslCertificates = verifySslCertificates;
        DataSourceType = dataSourceType;
        Path = path;
        ImportOptions = importOptions;
        Table = table;
        TargetFields = targetFields ?? new List<ImportTargetField>();
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Import Records [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ImportRecordsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ImportRecordsStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/import-records.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOrder" },
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySslCertificates" },
            // The data-source descriptor and path are emitted only when a
            // source is configured; the canonical unconfigured form omits them.
            new EnumValueChild("DataSourceType") { PocoProperty = "DataSourceTypeWire", Optional = true, ValidValues = ["File", "Folder", "XMLSource"], DefaultValue = "File" },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new Passthrough { PocoProperty = "ComplexWire" },
            new HrOnly("ImportOptions"),
            new HrOnly("Table"),
            new HrOnly("TargetFields"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
