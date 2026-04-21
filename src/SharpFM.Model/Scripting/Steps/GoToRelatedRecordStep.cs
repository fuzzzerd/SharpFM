using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to Related Record. Required Table reference (source relationship).
/// Optional Layout, ShowInNewWindow (mutually exclusive with Animation),
/// plus MatchAllRecords and Option flags. When ShowInNewWindow is True,
/// window geometry is carried by NewWndStyles.
/// </summary>
public sealed class GoToRelatedRecordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 74;
    public const string XmlName = "Go to Related Record";

    public bool ShowOnlyRelated { get; set; }
    public bool MatchAllRecords { get; set; }
    public bool ShowInNewWindow { get; set; }
    public bool RestoreWindowGeometry { get; set; }
    public string LayoutDestination { get; set; }
    public NewWindowStyles WindowStyles { get; set; }
    public NamedRef Table { get; set; }
    public NamedRef Layout { get; set; }
    public string Animation { get; set; }

    public GoToRelatedRecordStep(
        bool showOnlyRelated = false,
        bool matchAllRecords = false,
        bool showInNewWindow = false,
        bool restoreWindowGeometry = true,
        string layoutDestination = "SelectedLayout",
        NewWindowStyles? windowStyles = null,
        NamedRef? table = null,
        NamedRef? layout = null,
        string animation = "None",
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
        Layout = layout ?? new NamedRef(0, "");
        Animation = animation;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", ShowOnlyRelated ? "True" : "False")),
            new XElement("MatchAllRecords", new XAttribute("state", MatchAllRecords ? "True" : "False")),
            new XElement("ShowInNewWindow", new XAttribute("state", ShowInNewWindow ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreWindowGeometry ? "True" : "False")),
            new XElement("LayoutDestination", new XAttribute("value", LayoutDestination)),
            WindowStyles.ToXml(),
            Table.ToXml("Table"),
            Layout.ToXml("Layout"),
            new XElement("Animation", new XAttribute("value", Animation)));

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"From table: \"{Table.Name}\"");
        if (Layout.Id != 0 || !string.IsNullOrEmpty(Layout.Name))
            parts.Add($"Using layout: \"{Layout.Name}\"");
        if (ShowOnlyRelated) parts.Add("Show only related records");
        if (MatchAllRecords) parts.Add("Match found set");
        if (ShowInNewWindow) parts.Add("New window");
        return $"Go to Related Record [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var option = step.Element("Option")?.Attribute("state")?.Value == "True";
        var matchAll = step.Element("MatchAllRecords")?.Attribute("state")?.Value == "True";
        var newWin = step.Element("ShowInNewWindow")?.Attribute("state")?.Value == "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var dest = step.Element("LayoutDestination")?.Attribute("value")?.Value ?? "SelectedLayout";
        var stylesEl = step.Element("NewWndStyles");
        var styles = stylesEl is not null ? NewWindowStyles.FromXml(stylesEl) : NewWindowStyles.Default();
        var tableEl = step.Element("Table");
        var table = tableEl is not null ? NamedRef.FromXml(tableEl) : new NamedRef(0, "");
        var layoutEl = step.Element("Layout");
        var layout = layoutEl is not null ? NamedRef.FromXml(layoutEl) : new NamedRef(0, "");
        var animation = step.Element("Animation")?.Attribute("value")?.Value ?? "None";
        return new GoToRelatedRecordStep(option, matchAll, newWin, restore, dest, styles, table, layout, animation, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display form is lossy — NewWndStyles attrs, animation, and
        // LayoutDestination calc variants can't all round-trip.
        NamedRef table = new(0, "");
        NamedRef layout = new(0, "");
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
        return new GoToRelatedRecordStep(showOnly, matchAll, newWindow, true, "SelectedLayout", null, table, layout, "None", enabled);
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
            new ParamMetadata { Name = "Animation", XmlElement = "Animation", XmlAttr = "value", Type = "enum" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
