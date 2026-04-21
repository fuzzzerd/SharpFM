using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformAppleScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 67;
    public const string XmlName = "Perform AppleScript";

    public string ContentType { get; set; }
    public Calculation Calculation { get; set; }
    public string Text { get; set; }

    public PerformAppleScriptStep(
        string contentType = "Calculation",
        Calculation? calculation = null,
        string text = "",
        bool enabled = true)
        : base(null, enabled)
    {
        ContentType = contentType;
        Calculation = calculation ?? new Calculation("");
        Text = text;
    }

    private static readonly IReadOnlyDictionary<string, string> _ContentTypeToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Calculation"] = "Calculation",
        ["Text"] = "Text",
    };
    private static readonly IReadOnlyDictionary<string, string> _ContentTypeFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Calculation"] = "Calculation",
        ["Text"] = "Text",
    };
    private static string ContentTypeHr(string x) => _ContentTypeToHr.TryGetValue(x, out var h) ? h : x;
    private static string ContentTypeXml(string h) => _ContentTypeFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("ContentType", new XAttribute("value", ContentType)),
            Calculation.ToXml("Calculation"),
            new XElement("Text", Text));

    public override string ToDisplayLine() =>
        "Perform AppleScript [ " + ContentTypeHr(ContentType) + " ; " + Calculation.Text + " ; " + Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var contentType_v = step.Element("ContentType")?.Attribute("value")?.Value ?? "Calculation";
        var calculation_vEl = step.Element("Calculation");
        var calculation_v = calculation_vEl is not null ? Calculation.FromXml(calculation_vEl) : new Calculation("");
        var text_v = step.Element("Text")?.Value ?? "";
        return new PerformAppleScriptStep(contentType_v, calculation_v, text_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string contentType_v = "Calculation";
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        string text_v = "";
        return new PerformAppleScriptStep(contentType_v, calculation_v, text_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-applescript-os-x.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "ContentType",
                XmlElement = "ContentType",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["Calculation", "Text"],
                DefaultValue = "Calculation",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
            },
            new ParamMetadata
            {
                Name = "Text",
                XmlElement = "Text",
                Type = "text",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
