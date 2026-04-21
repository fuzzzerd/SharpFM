using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("Restore", new XAttribute("state", RestoreStoredOrder ? "True" : "False")),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySslCertificates ? "True" : "False")),
            new XElement("DataSourceType", new XAttribute("value", DataSourceType)),
            new XElement("UniversalPathList", Path));
        if (ImportOptions is not null)
        {
            var opts = new XElement("ImportOptions");
            foreach (var kv in ImportOptions) opts.Add(new XAttribute(kv.Key, kv.Value));
            step.Add(opts);
        }
        if (Table is not null) step.Add(Table.ToXml("Table"));
        if (TargetFields.Count > 0)
        {
            var tf = new XElement("TargetFields");
            foreach (var f in TargetFields) tf.Add(f.ToXml());
            step.Add(tf);
        }
        return step;
    }

    public override string ToDisplayLine() =>
        $"Import Records [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var optsEl = step.Element("ImportOptions");
        Dictionary<string, string>? opts = null;
        if (optsEl is not null)
        {
            opts = new Dictionary<string, string>();
            foreach (var a in optsEl.Attributes()) opts[a.Name.LocalName] = a.Value;
        }
        var tableEl = step.Element("Table");
        var targetEl = step.Element("TargetFields");
        return new ImportRecordsStep(
            step.Element("NoInteract")?.Attribute("state")?.Value != "True",
            step.Element("Restore")?.Attribute("state")?.Value == "True",
            step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True",
            step.Element("DataSourceType")?.Attribute("value")?.Value ?? "File",
            step.Element("UniversalPathList")?.Value ?? "",
            opts,
            tableEl is not null ? NamedRef.FromXml(tableEl) : null,
            targetEl?.Elements("Field").Select(ImportTargetField.FromXml).ToList(),
            enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ImportRecordsStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/import-records.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "VerifySSLCertificates", XmlElement = "VerifySSLCertificates", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "DataSourceType", XmlElement = "DataSourceType", XmlAttr = "value", Type = "enum", ValidValues = ["File", "Folder", "XMLSource"] },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "ImportOptions", XmlElement = "ImportOptions", Type = "complex" },
            new ParamMetadata { Name = "Table", XmlElement = "Table", Type = "tableRef" },
            new ParamMetadata { Name = "TargetFields", XmlElement = "TargetFields", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
