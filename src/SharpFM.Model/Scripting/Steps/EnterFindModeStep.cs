using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class EnterFindModeStep : ScriptStep, IStepFactory
{
    public const int XmlId = 22;
    public const string XmlName = "Enter Find Mode";

    public bool Pause { get; set; }
    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    public EnterFindModeStep(
        bool pause = true,
        bool restoreStoredRequests = true,
        FindRequestList? query = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Pause = pause;
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Pause", new XAttribute("state", Pause ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreStoredRequests ? "True" : "False")));
        if (Query is not null) step.Add(Query.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        $"Enter Find Mode [ Pause: {(Pause ? "On" : "Off")} ; Restore: {(RestoreStoredRequests ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var pause = step.Element("Pause")?.Attribute("state")?.Value == "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var qEl = step.Element("Query");
        var q = qEl is not null ? FindRequestList.FromXml(qEl) : null;
        return new EnterFindModeStep(pause, restore, q, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool pause = true, restore = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Pause:", System.StringComparison.OrdinalIgnoreCase))
                pause = t.Substring(6).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new EnterFindModeStep(pause, restore, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enter-find-mode.html",
        Params =
        [
            new ParamMetadata { Name = "Pause", XmlElement = "Pause", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Query", XmlElement = "Query", Type = "findRequests" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
