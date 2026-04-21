using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", OptionEnableTable ? "True" : "False")),
            new XElement("CreateDirectories", new XAttribute("state", CreateDirectories ? "True" : "False")),
            new XElement("FineTuneFormat", new XAttribute("state", FineTuneFormat ? "True" : "False")),
            new XElement("AutoOpen", new XAttribute("state", AutoOpen ? "True" : "False")),
            new XElement("CreateEmail", new XAttribute("state", CreateEmail ? "True" : "False")),
            new XElement("UniversalPathList", Path));
        if (CompletionField is not null) step.Add(CompletionField.ToXml("Field"));
        var bulk = new XElement("SaveAsJSONL");
        if (FineTuneFormat)
        {
            if (SystemPrompt is not null) bulk.Add(new XElement("SystemPrompt", SystemPrompt.ToXml("Calculation")));
            if (UserPrompt is not null) bulk.Add(new XElement("UserPrompt", UserPrompt.ToXml("Calculation")));
            if (AssistantPrompt is not null) bulk.Add(new XElement("AssistantPrompt", AssistantPrompt.ToXml("Calculation")));
        }
        else
        {
            if (SourceField is not null) bulk.Add(SourceField.ToXml("Field"));
        }
        step.Add(bulk);
        if (Table is not null) step.Add(Table.ToXml("Table"));
        return step;
    }

    public override string ToDisplayLine() =>
        $"Save Records as JSONL [ {Path} ; Format: {(FineTuneFormat ? "Fine-Tune" : "Completion")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        static Calculation? ReadCalc(XElement? parent) => parent?.Element("Calculation") is { } c ? Calculation.FromXml(c) : null;
        var bulk = step.Element("SaveAsJSONL");
        var topField = step.Element("Field");
        var tableEl = step.Element("Table");
        return new SaveRecordsAsJsonlStep(
            step.Element("Option")?.Attribute("state")?.Value == "True",
            step.Element("CreateDirectories")?.Attribute("state")?.Value == "True",
            step.Element("FineTuneFormat")?.Attribute("state")?.Value == "True",
            step.Element("AutoOpen")?.Attribute("state")?.Value == "True",
            step.Element("CreateEmail")?.Attribute("state")?.Value == "True",
            step.Element("UniversalPathList")?.Value ?? "",
            ReadCalc(bulk?.Element("SystemPrompt")),
            ReadCalc(bulk?.Element("UserPrompt")),
            ReadCalc(bulk?.Element("AssistantPrompt")),
            topField is not null ? FieldRef.FromXml(topField) : null,
            bulk?.Element("Field") is { } sf ? FieldRef.FromXml(sf) : null,
            tableEl is not null ? NamedRef.FromXml(tableEl) : null,
            enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new SaveRecordsAsJsonlStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
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
