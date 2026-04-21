using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EnableTouchKeyboardStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ShowHide value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class EnableTouchKeyboardStep : ScriptStep, IStepFactory
{
    public const int XmlId = 174;
    public const string XmlName = "Enable Touch Keyboard";

    /// <summary>The enum XML value emitted on the <c>&lt;ShowHide&gt;</c> element.</summary>
    public string ShowHide { get; set; }

    public EnableTouchKeyboardStep(string showHide = "Show", bool enabled = true)
        : base(enabled)
    {
        ShowHide = showHide;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["On"] = "On",
        ["Off"] = "Off",
        ["Toggle"] = "Toggle",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["On"] = "On",
        ["Off"] = "Off",
        ["Toggle"] = "Toggle",
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
            new XElement("ShowHide",
                new XAttribute("value", ShowHide)));

    public override string ToDisplayLine() =>
        $"Enable Touch Keyboard [ {ToHr(ShowHide)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("ShowHide")?.Attribute("value")?.Value ?? "Show";
        return new EnableTouchKeyboardStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        return new EnableTouchKeyboardStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enable-touch-keyboard.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "ShowHide",
                XmlElement = "ShowHide",
                Type = "enum",
                XmlAttr = "value",
                DefaultValue = "Show",
                ValidValues = ["On", "Off", "Toggle"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
