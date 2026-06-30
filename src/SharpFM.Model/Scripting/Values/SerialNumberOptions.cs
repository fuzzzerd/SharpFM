using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Config block for Replace Field Contents when the replacement mode is
/// SerialNumbers. Attributes round-trip losslessly; they control how FM
/// Pro interacts with the field's auto-enter serial options.
/// </summary>
public sealed record SerialNumberOptions(
    bool PerformAutoEnter,
    bool UpdateEntryOptions,
    bool UseEntryOptions,
    string Increment = "0",
    string InitialValue = "")
{
    public XElement ToXml() =>
        new("SerialNumbers",
            new XAttribute("PerformAutoEnter", PerformAutoEnter ? "True" : "False"),
            new XAttribute("UpdateEntryOptions", UpdateEntryOptions ? "True" : "False"),
            new XAttribute("increment", Increment),
            new XAttribute("InitialValue", InitialValue),
            new XAttribute("UseEntryOptions", UseEntryOptions ? "True" : "False"));

    public static SerialNumberOptions FromXml(XElement element) =>
        new(
            element.Attribute("PerformAutoEnter")?.Value == "True",
            element.Attribute("UpdateEntryOptions")?.Value == "True",
            element.Attribute("UseEntryOptions")?.Value == "True",
            element.Attribute("increment")?.Value ?? "0",
            element.Attribute("InitialValue")?.Value ?? "");
}
