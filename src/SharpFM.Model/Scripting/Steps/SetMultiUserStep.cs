using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetMultiUserStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;MultiUser value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class SetMultiUserStep : ScriptStep, IStepFactory
{
    public const int XmlId = 84;
    public const string XmlName = "Set Multi-User";

    /// <summary>The enum XML value emitted on the <c>&lt;MultiUser&gt;</c> element.</summary>
    public string NetworkAccess { get; set; }

    public SetMultiUserStep(string networkAccess = "True", bool enabled = true)
        : base(enabled)
    {
        NetworkAccess = networkAccess;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["True"] = "On",
        ["OnHidden"] = "On (Hidden)",
        ["False"] = "Off",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["On"] = "True",
        ["On (Hidden)"] = "OnHidden",
        ["Off"] = "False",
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
            new XElement("MultiUser",
                new XAttribute("value", NetworkAccess)));

    public override string ToDisplayLine() =>
        $"Set Multi-User [ Network access: {ToHr(NetworkAccess)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("MultiUser")?.Attribute("value")?.Value ?? "True";
        return new SetMultiUserStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Network access:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        return new SetMultiUserStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-multi-user.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "MultiUser",
                XmlElement = "MultiUser",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Network access",
                DefaultValue = "True",
                ValidValues = ["On", "On (Hidden)", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
