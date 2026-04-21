using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for RefreshWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class RefreshWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 80;
    public const string XmlName = "Refresh Window";

    public bool FlushCachedJoinResults { get; set; }
    public bool FlushCachedExternalData { get; set; }

    public RefreshWindowStep(
        bool flushCachedJoinResults = false,
        bool flushCachedExternalData = false,
        bool enabled = true)
        : base(enabled)
    {
        FlushCachedJoinResults = flushCachedJoinResults;
        FlushCachedExternalData = flushCachedExternalData;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", FlushCachedJoinResults ? "True" : "False")),
            new XElement("FlushSQLData", new XAttribute("state", FlushCachedExternalData ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Refresh Window [ " + "Flush cached join results: " + (FlushCachedJoinResults ? "On" : "Off") + " ; " + "Flush cached external data: " + (FlushCachedExternalData ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var flushCachedJoinResults_val = step.Element("Option")?.Attribute("state")?.Value == "True";
        var flushCachedExternalData_val = step.Element("FlushSQLData")?.Attribute("state")?.Value == "True";
        return new RefreshWindowStep(flushCachedJoinResults_val, flushCachedExternalData_val, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool flushCachedJoinResults_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Flush cached join results:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(26).Trim(); flushCachedJoinResults_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool flushCachedExternalData_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Flush cached external data:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(27).Trim(); flushCachedExternalData_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new RefreshWindowStep(flushCachedJoinResults_val, flushCachedExternalData_val, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-window.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Flush cached join results",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "FlushSQLData",
                XmlElement = "FlushSQLData",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Flush cached external data",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
