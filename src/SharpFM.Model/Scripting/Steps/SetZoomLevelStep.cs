using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetZoomLevelStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class SetZoomLevelStep : ScriptStep, IStepFactory
{
    public const int XmlId = 97;
    public const string XmlName = "Set Zoom Level";

    public bool Lock { get; set; }
    public string ZoomLevel { get; set; }

    public SetZoomLevelStep(
        bool @lock = false,
        string zoomLevel = "100",
        bool enabled = true)
        : base(enabled)
    {
        Lock = @lock;
        ZoomLevel = zoomLevel;
    }

    private static readonly IReadOnlyDictionary<string, string> _ZoomLevelXmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["25"] = "25%",
        ["50"] = "50%",
        ["75"] = "75%",
        ["100"] = "100%",
        ["150"] = "150%",
        ["200"] = "200%",
        ["300"] = "300%",
        ["400"] = "400%",
        ["ZoomIn"] = "Zoom In",
        ["ZoomOut"] = "Zoom Out",
    };

    private static readonly IReadOnlyDictionary<string, string> _ZoomLevelHrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["25%"] = "25",
        ["50%"] = "50",
        ["75%"] = "75",
        ["100%"] = "100",
        ["150%"] = "150",
        ["200%"] = "200",
        ["300%"] = "300",
        ["400%"] = "400",
        ["Zoom In"] = "ZoomIn",
        ["Zoom Out"] = "ZoomOut",
    };

    private static string ZoomLevelToHr(string x) =>
        _ZoomLevelXmlToHr.TryGetValue(x, out var h) ? h : x;

    private static string ZoomLevelFromHr(string h) =>
        _ZoomLevelHrToXml.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Lock", new XAttribute("state", Lock ? "True" : "False")),
            new XElement("Zoom", new XAttribute("value", ZoomLevel)));

    public override string ToDisplayLine() =>
        "Set Zoom Level [ " + "Lock: " + (Lock ? "On" : "Off") + " ; " + "Zoom level: " + ZoomLevelToHr(ZoomLevel) + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var @lock_val = step.Element("Lock")?.Attribute("state")?.Value == "True";
        var zoomLevel_val = step.Element("Zoom")?.Attribute("value")?.Value ?? "";
        return new SetZoomLevelStep(@lock_val, zoomLevel_val, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool @lock_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Lock:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(5).Trim(); @lock_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string zoomLevel_val = "100";
        foreach (var tok in tokens) { if (tok.StartsWith("Zoom level:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(11).Trim(); zoomLevel_val = ZoomLevelFromHr(v); break; } }
        return new SetZoomLevelStep(@lock_val, zoomLevel_val, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-zoom-level.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Lock",
                XmlElement = "Lock",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Lock",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Zoom",
                XmlElement = "Zoom",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Zoom level",
                ValidValues = ["25%", "50%", "75%", "100%", "150%", "200%", "300%", "400%", "Zoom In", "Zoom Out"],
                DefaultValue = "100",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
