using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Save Records as JSONL. Two modes driven by FineTuneFormat:
/// <list type="bullet">
/// <item><c>FineTuneFormat=True</c>: SaveAsJSONL carries System/User/Assistant prompt calcs.</item>
/// <item><c>FineTuneFormat=False</c>: Completion Field mode — a Field sibling outside SaveAsJSONL plus a Field inside it.</item>
/// </list>
/// We preserve whichever children are present.
/// </summary>
public sealed class SaveRecordsAsJsonlStep : ScriptStep, IStepFactory
{
    public const int XmlId = 225;
    public const string XmlName = "Save Records as JSONL";

    public bool OptionEnableTable { get; set; }
    public bool CreateDirectories { get; set; }
    public bool FineTuneFormat { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string Path { get; set; }
    public Calculation? SystemPrompt { get; set; }
    public Calculation? UserPrompt { get; set; }
    public Calculation? AssistantPrompt { get; set; }
    public FieldRef? CompletionField { get; set; }
    public FieldRef? SourceField { get; set; }
    public NamedRef? Table { get; set; }

    private SaveRecordsAsJsonlStep() : base(false) { Path = ""; }

    public SaveRecordsAsJsonlStep(
        bool optionEnableTable = false, bool createDirectories = false, bool fineTuneFormat = true,
        bool autoOpen = false, bool createEmail = false,
        string path = "",
        Calculation? systemPrompt = null, Calculation? userPrompt = null, Calculation? assistantPrompt = null,
        FieldRef? completionField = null, FieldRef? sourceField = null, NamedRef? table = null,
        bool enabled = true)
        : base(enabled)
    {
        OptionEnableTable = optionEnableTable;
        CreateDirectories = createDirectories;
        FineTuneFormat = fineTuneFormat;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        SystemPrompt = systemPrompt;
        UserPrompt = userPrompt;
        AssistantPrompt = assistantPrompt;
        CompletionField = completionField;
        SourceField = sourceField;
        Table = table;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Save Records as JSONL [ {Path} ; Format: {(FineTuneFormat ? "Fine-Tune" : "Completion")} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveRecordsAsJsonlStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new SaveRecordsAsJsonlStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        // Canonical order: five always-emitted flags, the optional path, an
        // optional top-level completion Field, the always-emitted SaveAsJSONL
        // wrapper (empty when unconfigured) holding the optional prompt calcs and
        // the inner source Field, then an optional Table. Each non-flag child is
        // Optional so the unconfigured form emits only the flags and empty wrapper.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "OptionEnableTable" },
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateDirectories" },
            new BoolStateChild("FineTuneFormat") { PocoProperty = "FineTuneFormat" },
            new BoolStateChild("AutoOpen") { PocoProperty = "AutoOpen" },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail" },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new FieldChild() { PocoProperty = "CompletionField", Optional = true },
            new WrapperChild("SaveAsJSONL",
            [
                new NamedCalcChild("SystemPrompt") { PocoProperty = "SystemPrompt", Optional = true },
                new NamedCalcChild("UserPrompt") { PocoProperty = "UserPrompt", Optional = true },
                new NamedCalcChild("AssistantPrompt") { PocoProperty = "AssistantPrompt", Optional = true },
                new FieldChild() { PocoProperty = "SourceField", Optional = true },
            ]),
            new NamedRefChild("Table") { PocoProperty = "Table", Optional = true },
        ],
        Params =
        [
            new ParamMetadata { Name = "Option", XmlElement = "Option", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateDirectories", XmlElement = "CreateDirectories", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "FineTuneFormat", XmlElement = "FineTuneFormat", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "AutoOpen", XmlElement = "AutoOpen", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "CreateEmail", XmlElement = "CreateEmail", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "SaveAsJSONL", XmlElement = "SaveAsJSONL", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
