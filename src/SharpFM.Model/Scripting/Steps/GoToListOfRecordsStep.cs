using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to List of Records. Carries a calc producing the list of record
/// IDs (list of values, JSON array, or array of <c>{recordId}</c> objects),
/// a layout destination enum, and window geometry for the new-window mode.
/// </summary>
public sealed class GoToListOfRecordsStep : ScriptStep<GoToListOfRecordsStep>, IStepFactory
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

    // Hand-written: shows RowList/Layout in the reverse of canonical XML order
    // and "New window" as a conditional bare token a BoolStateChild cannot render.
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

    // Hand-written: display is lossy (RowList is the only parseable field) and
    // an unlabeled shape parse would mis-bind the labeled Records/Layout tokens.
    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        Calculation rowList = new("");
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Records:", System.StringComparison.OrdinalIgnoreCase))
                rowList = new Calculation(t.Substring(8).Trim());
        }
        ShowInNewWindow = false;
        LayoutDestination = "CurrentLayout";
        RowList = rowList;
        WindowStyles = NewWindowStyles.Default();
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
            new BoolStateChild("ShowInNewWindow") { PocoProperty = "ShowInNewWindow", Display = DisplayMode.Native },
            new EnumValueChild("LayoutDestination") { PocoProperty = "LayoutDestination", DefaultValue = "CurrentLayout", Display = DisplayMode.Native },
            new NamedCalcChild("RowList") { PocoProperty = "RowList", Optional = true, Display = DisplayMode.Native },
            new ValueTypeChild("NewWndStyles") { PocoProperty = "WindowStyles", Display = DisplayMode.Native },
        ],
    };
}
