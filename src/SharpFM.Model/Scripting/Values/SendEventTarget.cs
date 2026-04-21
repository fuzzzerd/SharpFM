using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// The <c>&lt;Event&gt;</c> sub-element of the Send Event step. Carries
/// target app, event class/id, and three booleans controlling behavior.
/// All attributes round-trip losslessly.
/// </summary>
public sealed record SendEventTarget(
    bool CopyResultToClipboard,
    bool WaitForCompletion,
    bool BringTargetToForeground,
    string TargetType,
    string TargetName,
    string Id,
    string Class)
{
    public XElement ToXml() =>
        new("Event",
            new XAttribute("CopyResultToClipboard", CopyResultToClipboard ? "True" : "False"),
            new XAttribute("WaitForCompletion", WaitForCompletion ? "True" : "False"),
            new XAttribute("BringTargetToForeground", BringTargetToForeground ? "True" : "False"),
            new XAttribute("TargetType", TargetType),
            new XAttribute("TargetName", TargetName),
            new XAttribute("id", Id),
            new XAttribute("class", Class));

    public static SendEventTarget FromXml(XElement element) =>
        new(
            element.Attribute("CopyResultToClipboard")?.Value == "True",
            element.Attribute("WaitForCompletion")?.Value == "True",
            element.Attribute("BringTargetToForeground")?.Value == "True",
            element.Attribute("TargetType")?.Value ?? "",
            element.Attribute("TargetName")?.Value ?? "",
            element.Attribute("id")?.Value ?? "",
            element.Attribute("class")?.Value ?? "");

    public static SendEventTarget Default() => new(false, true, false, "NUTD", "<unknown>", "dosc", "misc");
}
