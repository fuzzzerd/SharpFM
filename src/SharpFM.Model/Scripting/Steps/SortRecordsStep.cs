using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SortRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 39;
    public const string XmlName = "Sort Records";

    public bool WithDialog { get; set; }
    public bool RestoreStoredOrder { get; set; }
    public SortList? Sort { get; set; }

    public SortRecordsStep(
        bool withDialog = false,
        bool restoreStoredOrder = true,
        SortList? sort = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        RestoreStoredOrder = restoreStoredOrder;
        Sort = sort;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("Restore", new XAttribute("state", RestoreStoredOrder ? "True" : "False")));
        if (Sort is not null) step.Add(Sort.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        $"Sort Records [ With dialog: {(WithDialog ? "On" : "Off")} ; Restore: {(RestoreStoredOrder ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var sortEl = step.Element("SortList");
        var sort = sortEl is not null ? SortList.FromXml(sortEl) : null;
        return new SortRecordsStep(withDialog, restore, sort, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = false, restore = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", System.StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new SortRecordsStep(withDialog, restore, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/sort-records.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "SortList", XmlElement = "SortList", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
