using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to List of Records. Carries a calc producing the list of record
/// IDs (list of values, JSON array, or array of <c>{recordId}</c> objects),
/// a layout destination enum, and window geometry for the new-window mode.
/// </summary>
public sealed class GoToListOfRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 228;
    public const string XmlName = "Go to List of Records";

    public bool ShowInNewWindow { get; set; }
    public string LayoutDestination { get; set; } = "CurrentLayout";
    public Calculation? RowList { get; set; }
    public NewWindowStyles WindowStyles { get; set; } = NewWindowStyles.Default();

    private GoToListOfRecordsStep() : base(false) { }

    public GoToListOfRecordsStep(
        bool showInNewWindow = false,
        string layoutDestination = "CurrentLayout",
        Calculation? rowList = null,
        NewWindowStyles? windowStyles = null,
        bool enabled = true)
        : base(enabled)
    {
        ShowInNewWindow = showInNewWindow;
        LayoutDestination = layoutDestination;
        RowList = rowList;
        WindowStyles = windowStyles ?? NewWindowStyles.Default();
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Records: {RowList?.Text ?? ""}",
            $"Layout: {LayoutDestination}",
        };
        if (ShowInNewWindow) parts.Add("New window");
        return $"Go to List of Records [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GoToListOfRecordsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Lossy display — RowList calc is the only field meaningful to parse.
        Calculation rowList = new("");
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Records:", System.StringComparison.OrdinalIgnoreCase))
                rowList = new Calculation(t.Substring(8).Trim());
        }
        return new GoToListOfRecordsStep(false, "CurrentLayout", rowList, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-list-of-records.html",
        // Canonical: ShowInNewWindow, LayoutDestination, the optional <RowList>
        // calculation, then the NewWndStyles value type.
        Shape =
        [
            new BoolStateChild("ShowInNewWindow") { PocoProperty = "ShowInNewWindow", Display = DisplayMode.Hidden },
            new EnumValueChild("LayoutDestination") { PocoProperty = "LayoutDestination", HrLabel = "Layout", DefaultValue = "CurrentLayout", Display = DisplayMode.Native },
            new NamedCalcChild("RowList") { PocoProperty = "RowList", HrLabel = "Records", Optional = true, Display = DisplayMode.Native },
            new ValueTypeChild("NewWndStyles") { PocoProperty = "WindowStyles", Display = DisplayMode.Hidden },
        ],
        Params =
        [
            new ParamMetadata { Name = "ShowInNewWindow", XmlElement = "ShowInNewWindow", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "LayoutDestination", XmlElement = "LayoutDestination", XmlAttr = "value", Type = "enum" },
            new ParamMetadata { Name = "RowList", XmlElement = "RowList", Type = "namedCalc", Required = true },
            new ParamMetadata { Name = "NewWndStyles", XmlElement = "NewWndStyles", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
