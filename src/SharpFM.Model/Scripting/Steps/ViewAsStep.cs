using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ViewAsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;View value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ViewAsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 30;
    public const string XmlName = "View As";

    /// <summary>The enum XML value emitted on the <c>&lt;View&gt;</c> element.</summary>
    public string View { get; set; }

    public ViewAsStep(string view = "Cycle", bool enabled = true)
        : base(null, enabled)
    {
        View = view;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Cycle"] = "Cycle",
        ["Form"] = "View as Form",
        ["List"] = "View as List",
        ["Table"] = "View as Table",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Cycle"] = "Cycle",
        ["View as Form"] = "Form",
        ["View as List"] = "List",
        ["View as Table"] = "Table",
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
            new XElement("View",
                new XAttribute("value", View)));

    public override string ToDisplayLine() =>
        $"View As [ View: {ToHr(View)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("View")?.Attribute("value")?.Value ?? "Cycle";
        return new ViewAsStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "View:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        return new ViewAsStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/view-as.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "View",
                XmlElement = "View",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "View",
                DefaultValue = "Cycle",
                ValidValues = ["Cycle", "View as Form", "View as List", "View as Table"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
