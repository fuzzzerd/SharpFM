using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConstrainFoundSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 126;
    public const string XmlName = "Constrain Found Set";

    public bool WithoutIndexes { get; set; }
    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    public ConstrainFoundSetStep(
        bool withoutIndexes = true,
        bool restoreStoredRequests = true,
        FindRequestList? query = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WithoutIndexes = withoutIndexes;
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", WithoutIndexes ? "True" : "False")),
            new XElement("Restore", new XAttribute("state", RestoreStoredRequests ? "True" : "False")));
        if (Query is not null) step.Add(Query.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        $"Constrain Found Set [ Restore: {(RestoreStoredRequests ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var option = step.Element("Option")?.Attribute("state")?.Value == "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var qEl = step.Element("Query");
        var q = qEl is not null ? FindRequestList.FromXml(qEl) : null;
        return new ConstrainFoundSetStep(option, restore, q, enabled);
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
        return new ConstrainFoundSetStep(true, restore, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/constrain-found-set.html",
        Params =
        [
            new ParamMetadata { Name = "Option", XmlElement = "Option", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Query", XmlElement = "Query", Type = "findRequests" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
