using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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
    public string LayoutDestination { get; set; }
    public Calculation RowList { get; set; }
    public NewWindowStyles WindowStyles { get; set; }

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
        RowList = rowList ?? new Calculation("");
        WindowStyles = windowStyles ?? NewWindowStyles.Default();
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("ShowInNewWindow", new XAttribute("state", ShowInNewWindow ? "True" : "False")),
            new XElement("LayoutDestination", new XAttribute("value", LayoutDestination)),
            new XElement("RowList", RowList.ToXml("Calculation")),
            WindowStyles.ToXml());

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Records: {RowList.Text}",
            $"Layout: {LayoutDestination}",
        };
        if (ShowInNewWindow) parts.Add("New window");
        return $"Go to List of Records [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var newWin = step.Element("ShowInNewWindow")?.Attribute("state")?.Value == "True";
        var dest = step.Element("LayoutDestination")?.Attribute("value")?.Value ?? "CurrentLayout";
        var listEl = step.Element("RowList")?.Element("Calculation");
        var rowList = listEl is not null ? Calculation.FromXml(listEl) : new Calculation("");
        var stylesEl = step.Element("NewWndStyles");
        var styles = stylesEl is not null ? NewWindowStyles.FromXml(stylesEl) : NewWindowStyles.Default();
        return new GoToListOfRecordsStep(newWin, dest, rowList, styles, enabled);
    }

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
