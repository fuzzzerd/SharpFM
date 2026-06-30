using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to Related Record (74). Canonical form (skill, navigation reference):
/// Option, MatchAllRecords, ShowInNewWindow and Restore flags, then
/// LayoutDestination, the NewWndStyles attribute element, the always-present
/// Table reference, and an optional Layout. The skill documents no
/// <c>&lt;Animation&gt;</c> element for this step, so SharpFM no longer emits one.
/// </summary>
public sealed class GoToRelatedRecordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 74;
    public const string XmlName = "Go to Related Record";

    public bool ShowOnlyRelated { get; set; }
    public bool MatchAllRecords { get; set; }
    public bool ShowInNewWindow { get; set; }
    public bool RestoreWindowGeometry { get; set; }
    public string LayoutDestination { get; set; } = "SelectedLayout";
    public NewWindowStyles WindowStyles { get; set; } = NewWindowStyles.Default();
    public NamedRef Table { get; set; } = new(0, "");
    public NamedRef? Layout { get; set; }

    private GoToRelatedRecordStep() : base(false) { }

    public GoToRelatedRecordStep(
        bool showOnlyRelated = false,
        bool matchAllRecords = false,
        bool showInNewWindow = false,
        bool restoreWindowGeometry = true,
        string layoutDestination = "SelectedLayout",
        NewWindowStyles? windowStyles = null,
        NamedRef? table = null,
        NamedRef? layout = null,
        bool enabled = true)
        : base(enabled)
    {
        ShowOnlyRelated = showOnlyRelated;
        MatchAllRecords = matchAllRecords;
        ShowInNewWindow = showInNewWindow;
        RestoreWindowGeometry = restoreWindowGeometry;
        LayoutDestination = layoutDestination;
        WindowStyles = windowStyles ?? NewWindowStyles.Default();
        Table = table ?? new NamedRef(0, "");
        Layout = layout;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"From table: \"{Table.Name}\"");
        if (Layout is not null && (Layout.Id != 0 || !string.IsNullOrEmpty(Layout.Name)))
            parts.Add($"Using layout: \"{Layout.Name}\"");
        if (ShowOnlyRelated) parts.Add("Show only related records");
        if (MatchAllRecords) parts.Add("Match found set");
        if (ShowInNewWindow) parts.Add("New window");
        return $"Go to Related Record [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GoToRelatedRecordStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display form is lossy — NewWndStyles attrs and LayoutDestination calc
        // variants can't all round-trip.
        NamedRef table = new(0, "");
        NamedRef? layout = null;
        bool showOnly = false, matchAll = false, newWindow = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("From table:", StringComparison.OrdinalIgnoreCase))
                table = new NamedRef(0, Unquote(t.Substring(11).Trim()));
            else if (t.StartsWith("Using layout:", StringComparison.OrdinalIgnoreCase))
                layout = new NamedRef(0, Unquote(t.Substring(13).Trim()));
            else if (t.Equals("Show only related records", StringComparison.OrdinalIgnoreCase))
                showOnly = true;
            else if (t.Equals("Match found set", StringComparison.OrdinalIgnoreCase))
                matchAll = true;
            else if (t.Equals("New window", StringComparison.OrdinalIgnoreCase))
                newWindow = true;
        }
        return new GoToRelatedRecordStep(showOnly, matchAll, newWindow, true, "SelectedLayout", null, table, layout, enabled);
    }

    private static string Unquote(string s)
    {
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2) return s.Substring(1, s.Length - 2);
        return s;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-related-record.html",
        // Canonical order: Option, MatchAllRecords, ShowInNewWindow, Restore,
        // LayoutDestination, NewWndStyles, Table (always), optional Layout.
        // No <Animation> for this step (skill).
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "ShowOnlyRelated", HrLabel = "Show only related records", Display = DisplayMode.Hidden },
            new BoolStateChild("MatchAllRecords") { PocoProperty = "MatchAllRecords", HrLabel = "Match found set", Display = DisplayMode.Hidden },
            new BoolStateChild("ShowInNewWindow") { PocoProperty = "ShowInNewWindow", HrLabel = "New window", Display = DisplayMode.Hidden },
            new BoolStateChild("Restore") { PocoProperty = "RestoreWindowGeometry", Display = DisplayMode.Hidden },
            new EnumValueChild("LayoutDestination") { PocoProperty = "LayoutDestination", DefaultValue = "SelectedLayout", Display = DisplayMode.Hidden },
            new ValueTypeChild("NewWndStyles") { PocoProperty = "WindowStyles", Display = DisplayMode.Hidden },
            new NamedRefChild("Table") { PocoProperty = "Table", Required = true, Display = DisplayMode.Native },
            new NamedRefChild("Layout") { PocoProperty = "Layout", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata { Name = "Option", XmlElement = "Option", XmlAttr = "state", Type = "boolean", HrLabel = "Show only related records" },
            new ParamMetadata { Name = "MatchAllRecords", XmlElement = "MatchAllRecords", XmlAttr = "state", Type = "boolean", HrLabel = "Match found set" },
            new ParamMetadata { Name = "ShowInNewWindow", XmlElement = "ShowInNewWindow", XmlAttr = "state", Type = "boolean", HrLabel = "New window" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "LayoutDestination", XmlElement = "LayoutDestination", XmlAttr = "value", Type = "enum", ValidValues = ["CurrentLayout", "SelectedLayout", "LayoutNameByCalc", "LayoutNumberByCalc", "UseExternalTableLayouts"] },
            new ParamMetadata { Name = "Table", XmlElement = "Table", Type = "tableOccurrence", Required = true },
            new ParamMetadata { Name = "Layout", XmlElement = "Layout", Type = "layoutRef" },
            new ParamMetadata { Name = "NewWndStyles", XmlElement = "NewWndStyles", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
