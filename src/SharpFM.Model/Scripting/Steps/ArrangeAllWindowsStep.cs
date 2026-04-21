using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ArrangeAllWindowsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;WindowArrangement value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ArrangeAllWindowsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 120;
    public const string XmlName = "Arrange All Windows";

    /// <summary>The enum XML value emitted on the <c>&lt;WindowArrangement&gt;</c> element.</summary>
    public string WindowArrangement { get; set; }

    public ArrangeAllWindowsStep(string windowArrangement = "Cascade Window", bool enabled = true)
        : base(enabled)
    {
        WindowArrangement = windowArrangement;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Tile Horizontally"] = "Tile Horizontally",
        ["Tile Vertically"] = "Tile Vertically",
        ["Cascade Window"] = "Cascade Window",
        ["Bring All To Front"] = "Bring All To Front",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Tile Horizontally"] = "Tile Horizontally",
        ["Tile Vertically"] = "Tile Vertically",
        ["Cascade Window"] = "Cascade Window",
        ["Bring All To Front"] = "Bring All To Front",
    };

    private static string ToHr(string xmlValue) =>
        _xmlToHr.TryGetValue(xmlValue, out var hr) ? hr : xmlValue;

    private static string FromHr(string hrValue) =>
        _hrToXml.TryGetValue(hrValue, out var xml) ? xml : hrValue;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("WindowArrangement",
                new XAttribute("value", WindowArrangement)));

    public override string ToDisplayLine() =>
        $"Arrange All Windows [ {ToHr(WindowArrangement)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("WindowArrangement")?.Attribute("value")?.Value ?? "Cascade Window";
        return new ArrangeAllWindowsStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        return new ArrangeAllWindowsStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/arrange-all-windows.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "WindowArrangement",
                XmlElement = "WindowArrangement",
                Type = "enum",
                XmlAttr = "value",
                DefaultValue = "Cascade Window",
                ValidValues = ["Tile Horizontally", "Tile Vertically", "Cascade Window", "Bring All To Front"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
