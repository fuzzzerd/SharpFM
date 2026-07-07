using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformAppleScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 67;
    public const string XmlName = "Perform AppleScript";

    public string ContentType { get; set; }
    public Calculation? Calculation { get; set; }
    public string Text { get; set; }

    private PerformAppleScriptStep() : base(false) { ContentType = "Calculation"; Text = ""; }

    public PerformAppleScriptStep(
        string contentType = "Calculation",
        Calculation? calculation = null,
        string text = "",
        bool enabled = true)
        : base(enabled)
    {
        ContentType = contentType;
        Calculation = calculation;
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Perform AppleScript [ " + ContentTypeHr(ContentType) + " ; " + (Calculation?.Text ?? "") + " ; " + Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformAppleScriptStep>(step, Metadata);

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
        // Canonical 067-PerformAppleScript: only <ContentType>; the bare
        // <Calculation> and the <Text> element are omitted until configured,
        // so both are Optional.
        Shape =
        [
            new EnumValueChild("ContentType") { PocoProperty = "ContentType", DefaultValue = "Calculation", DisplayValues = ["Calculation", "Text"] },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true },
            new NamedTextChild("Text") { PocoProperty = "Text", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
