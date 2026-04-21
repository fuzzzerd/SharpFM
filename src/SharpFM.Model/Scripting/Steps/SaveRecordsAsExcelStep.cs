using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Save Records as Excel. Profile carries export format metadata
/// (FieldDelimiter, IsPredefined, FieldNameRow, DataType); the four
/// doc-metadata calcs (WorkSheet, Title, Subject, Author) wrap
/// Calculation children and are optional.
/// </summary>
public sealed class SaveRecordsAsExcelStep : ScriptStep, IStepFactory
{
    public const int XmlId = 143;
    public const string XmlName = "Save Records as Excel";

    public bool WithDialog { get; set; }
    public bool CreateDirectories { get; set; }
    public bool RestoreStoredOptions { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string FieldDelimiter { get; set; }
    public string IsPredefined { get; set; }
    public string FieldNameRow { get; set; }
    public string DataType { get; set; }
    public string Path { get; set; }
    public Calculation? WorkSheet { get; set; }
    public Calculation? Title { get; set; }
    public Calculation? Subject { get; set; }
    public Calculation? Author { get; set; }
    public string SaveType { get; set; }
    public bool UseFieldNames { get; set; }

    public SaveRecordsAsExcelStep(
        bool withDialog = false, bool createDirectories = true, bool restoreStoredOptions = true,
        bool autoOpen = false, bool createEmail = false,
        string fieldDelimiter = "\t", string isPredefined = "-1", string fieldNameRow = "-1", string dataType = "XLXE",
        string path = "",
        Calculation? workSheet = null, Calculation? title = null, Calculation? subject = null, Calculation? author = null,
        string saveType = "BrowsedRecords", bool useFieldNames = false,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        CreateDirectories = createDirectories;
        RestoreStoredOptions = restoreStoredOptions;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        FieldDelimiter = fieldDelimiter;
        IsPredefined = isPredefined;
        FieldNameRow = fieldNameRow;
        DataType = dataType;
        Path = path;
        WorkSheet = workSheet;
        Title = title;
        Subject = subject;
        Author = author;
        SaveType = saveType;
        UseFieldNames = useFieldNames;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreStoredOptions ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("Profile",
                new XAttribute("FieldDelimiter", FieldDelimiter),
                new XAttribute("IsPredefined", IsPredefined),
                new XAttribute("FieldNameRow", FieldNameRow),
                new XAttribute("DataType", DataType)),
            new XElement("UniversalPathList", Path));
        if (WorkSheet is not null) step.Add(new XElement("WorkSheet", WorkSheet.ToXml("Calculation")));
        if (Title is not null) step.Add(new XElement("Title", Title.ToXml("Calculation")));
        if (Subject is not null) step.Add(new XElement("Subject", Subject.ToXml("Calculation")));
        if (Author is not null) step.Add(new XElement("Author", Author.ToXml("Calculation")));
        step.Add(new XElement("SaveType", new XAttribute("value", SaveType)));
        step.Add(new XElement("UseFieldNames", new XAttribute("state", UseFieldNames ? "True" : "False")));
        return step;
    }

    public override string ToDisplayLine() =>
        $"Save Records as Excel [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        static Calculation? ReadCalc(XElement? parent) => parent?.Element("Calculation") is { } c ? Calculation.FromXml(c) : null;
        var profile = step.Element("Profile");
        return new SaveRecordsAsExcelStep(
            step.Element("NoInteract")?.Attribute("state")?.Value != "True",
            step.Element("CreateDirectories")?.Attribute("state")?.Value == "True",
            step.Element("Restore")?.Attribute("state")?.Value == "True",
            step.Element("AutoOpen")?.Attribute("state")?.Value == "True",
            step.Element("CreateEmail")?.Attribute("state")?.Value == "True",
            profile?.Attribute("FieldDelimiter")?.Value ?? "\t",
            profile?.Attribute("IsPredefined")?.Value ?? "-1",
            profile?.Attribute("FieldNameRow")?.Value ?? "-1",
            profile?.Attribute("DataType")?.Value ?? "XLXE",
            step.Element("UniversalPathList")?.Value ?? "",
            ReadCalc(step.Element("WorkSheet")),
            ReadCalc(step.Element("Title")),
            ReadCalc(step.Element("Subject")),
            ReadCalc(step.Element("Author")),
            step.Element("SaveType")?.Attribute("value")?.Value ?? "BrowsedRecords",
            step.Element("UseFieldNames")?.Attribute("state")?.Value == "True",
            enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new SaveRecordsAsExcelStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-records-as-excel.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Profile", XmlElement = "Profile", Type = "complex" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "WorkSheet", XmlElement = "WorkSheet", Type = "namedCalc" },
            new ParamMetadata { Name = "Title", XmlElement = "Title", Type = "namedCalc" },
            new ParamMetadata { Name = "Subject", XmlElement = "Subject", Type = "namedCalc" },
            new ParamMetadata { Name = "Author", XmlElement = "Author", Type = "namedCalc" },
            new ParamMetadata { Name = "SaveType", XmlElement = "SaveType", XmlAttr = "value", Type = "enum", ValidValues = ["BrowsedRecords", "CurrentRecord"] },
            new ParamMetadata { Name = "UseFieldNames", XmlElement = "UseFieldNames", XmlAttr = "state", Type = "boolean" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
