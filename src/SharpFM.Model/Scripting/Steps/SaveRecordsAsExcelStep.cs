using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// Shape-facing view of the <c>&lt;Profile&gt;</c> element (a multi-attribute
    /// element no shape primitive models): emitted only when the export format
    /// differs from the defaults, and parsed back through the shape's
    /// passthrough slot.
    /// </summary>
    public List<XElement> ProfileWire
    {
        get
        {
            var profileDefault = FieldDelimiter == "\t" && IsPredefined == "-1"
                && FieldNameRow == "-1" && DataType == "XLXE";
            return profileDefault
                ? []
                :
                [
                    new XElement("Profile",
                        new XAttribute("FieldDelimiter", FieldDelimiter),
                        new XAttribute("IsPredefined", IsPredefined),
                        new XAttribute("FieldNameRow", FieldNameRow),
                        new XAttribute("DataType", DataType)),
                ];
        }
        set
        {
            var profile = value.FirstOrDefault(e => e.Name.LocalName == "Profile");
            FieldDelimiter = profile?.Attribute("FieldDelimiter")?.Value ?? "\t";
            IsPredefined = profile?.Attribute("IsPredefined")?.Value ?? "-1";
            FieldNameRow = profile?.Attribute("FieldNameRow")?.Value ?? "-1";
            DataType = profile?.Attribute("DataType")?.Value ?? "XLXE";
        }
    }

    private SaveRecordsAsExcelStep() : this(enabled: true) { }

    public SaveRecordsAsExcelStep(
        bool withDialog = false, bool createDirectories = true, bool restoreStoredOptions = true,
        bool autoOpen = false, bool createEmail = false,
        string fieldDelimiter = "\t", string isPredefined = "-1", string fieldNameRow = "-1", string dataType = "XLXE",
        string path = "",
        Calculation? workSheet = null, Calculation? title = null, Calculation? subject = null, Calculation? author = null,
        string saveType = "BrowsedRecords", bool useFieldNames = false,
        bool enabled = true)
        : base(enabled)
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Save Records as Excel [ With dialog: {(WithDialog ? "On" : "Off")} ; {Path} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveRecordsAsExcelStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new SaveRecordsAsExcelStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-records-as-excel.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("CreateDirectories"),
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOptions" },
            new BoolStateChild("AutoOpen"),
            new BoolStateChild("CreateEmail"),
            // <Profile> is a multi-attribute element no primitive models; the
            // ProfileWire view emits it only when configured and parses it
            // back through this passthrough slot.
            new Passthrough { PocoProperty = "ProfileWire" },
            new HrOnly("Profile"),
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new NamedCalcChild("WorkSheet") { Optional = true },
            new NamedCalcChild("Title") { Optional = true },
            new NamedCalcChild("Subject") { Optional = true },
            new NamedCalcChild("Author") { Optional = true },
            new EnumValueChild("SaveType") { ValidValues = ["BrowsedRecords", "CurrentRecord"], DefaultValue = "BrowsedRecords" },
            new BoolStateChild("UseFieldNames"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
