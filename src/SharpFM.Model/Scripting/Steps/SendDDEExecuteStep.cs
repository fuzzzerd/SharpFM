using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SendDDEExecuteStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ContentType value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class SendDDEExecuteStep : ScriptStep, IStepFactory
{
    public const int XmlId = 64;
    public const string XmlName = "Send DDE Execute";

    /// <summary>The enum XML value emitted on the <c>&lt;ContentType&gt;</c> element.</summary>
    public string ContentType { get; set; }

    public SendDDEExecuteStep(string contentType = "File", bool enabled = true)
        : base(null, enabled)
    {
        ContentType = contentType;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["File"] = "File",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["File"] = "File",
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
            new XElement("ContentType",
                new XAttribute("value", ContentType)));

    public override string ToDisplayLine() =>
        $"Send DDE Execute [ {ToHr(ContentType)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("ContentType")?.Attribute("value")?.Value ?? "File";
        return new SendDDEExecuteStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        return new SendDDEExecuteStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/send-dde-execute-windows.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "ContentType",
                XmlElement = "ContentType",
                Type = "enum",
                XmlAttr = "value",
                DefaultValue = "File",
                ValidValues = ["File"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
