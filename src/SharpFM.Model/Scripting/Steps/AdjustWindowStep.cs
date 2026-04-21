using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AdjustWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;WindowState value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class AdjustWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 31;
    public const string XmlName = "Adjust Window";

    /// <summary>The enum XML value emitted on the <c>&lt;WindowState&gt;</c> element.</summary>
    public string WindowState { get; set; }

    public AdjustWindowStep(string windowState = "ResizeToFit", bool enabled = true)
        : base(enabled)
    {
        WindowState = windowState;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Resize to Fit"] = "Resize to Fit",
        ["Maximize"] = "Maximize",
        ["Minimize"] = "Minimize",
        ["Restore"] = "Restore",
        ["Hide"] = "Hide",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Resize to Fit"] = "Resize to Fit",
        ["Maximize"] = "Maximize",
        ["Minimize"] = "Minimize",
        ["Restore"] = "Restore",
        ["Hide"] = "Hide",
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
            new XElement("WindowState",
                new XAttribute("value", WindowState)));

    public override string ToDisplayLine() =>
        $"Adjust Window [ {ToHr(WindowState)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("WindowState")?.Attribute("value")?.Value ?? "ResizeToFit";
        return new AdjustWindowStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        return new AdjustWindowStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/adjust-window.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "WindowState",
                XmlElement = "WindowState",
                Type = "enum",
                XmlAttr = "value",
                DefaultValue = "ResizeToFit",
                ValidValues = ["Resize to Fit", "Maximize", "Minimize", "Restore", "Hide"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
