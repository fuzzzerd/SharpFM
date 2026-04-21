using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExtendFoundSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 127;
    public const string XmlName = "Extend Found Set";

    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    public ExtendFoundSetStep(bool restoreStoredRequests = true, FindRequestList? query = null, bool enabled = true)
        : base(enabled)
    {
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Restore", new XAttribute("state", RestoreStoredRequests ? "True" : "False")));
        if (Query is not null) step.Add(Query.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        $"Extend Found Set [ Restore: {(RestoreStoredRequests ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var qEl = step.Element("Query");
        var q = qEl is not null ? FindRequestList.FromXml(qEl) : null;
        return new ExtendFoundSetStep(restore, q, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool restore = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new ExtendFoundSetStep(restore, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/extend-found-set.html",
        Params =
        [
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Query", XmlElement = "Query", Type = "findRequests" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
