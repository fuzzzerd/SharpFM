using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ScrollWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ScrollOperation value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ScrollWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 81;
    public const string XmlName = "Scroll Window";

    /// <summary>The enum XML value emitted on the <c>&lt;ScrollOperation&gt;</c> element.</summary>
    public string Direction { get; set; } = "Home";

    private ScrollWindowStep() : base(false) { }

    public ScrollWindowStep(string direction = "Home", bool enabled = true)
        : base(enabled)
    {
        Direction = direction;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Home"] = "Home",
        ["End"] = "End",
        ["PageUp"] = "Page Up",
        ["PageDown"] = "Page Down",
        ["ToSelection"] = "To Selection",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = "Home",
        ["End"] = "End",
        ["Page Up"] = "PageUp",
        ["Page Down"] = "PageDown",
        ["To Selection"] = "ToSelection",
    };

    private static string ToHr(string xmlValue) =>
        _xmlToHr.TryGetValue(xmlValue, out var hr) ? hr : xmlValue;

    private static string FromHr(string hrValue) =>
        _hrToXml.TryGetValue(hrValue, out var xml) ? xml : hrValue;

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Scroll Window [ Direction: {ToHr(Direction)} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ScrollWindowStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Direction:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        return new ScrollWindowStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/scroll-window.html",
        // Canonical: a single ScrollOperation enum child.
        Shape =
        [
            new EnumValueChild("ScrollOperation")
            {
                PocoProperty = "Direction",
                HrLabel = "Direction",
                DefaultValue = "Home",
                ValidValues = ["Home", "End", "PageUp", "PageDown", "ToSelection"],
                Display = DisplayMode.Native,
            },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "ScrollOperation",
                XmlElement = "ScrollOperation",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Direction",
                DefaultValue = "Home",
                ValidValues = ["Home", "End", "Page Up", "Page Down", "To Selection"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
