using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Operation attributes for the Perform Find/Replace step. Controls find
/// direction, case/word matching, and which find or replace action to
/// perform.
/// </summary>
public sealed record FindReplaceOperation(
    string Type,
    string Direction,
    bool MatchWholeWords,
    bool MatchCase,
    string WithinOptions,
    string AcrossOptions)
{
    public XElement ToXml() =>
        new("FindReplaceOperation",
            new XAttribute("MatchWholeWords", MatchWholeWords ? "True" : "False"),
            new XAttribute("MatchCase", MatchCase ? "True" : "False"),
            new XAttribute("WithinOptions", WithinOptions),
            new XAttribute("AcrossOptions", AcrossOptions),
            new XAttribute("direction", Direction),
            new XAttribute("type", Type));

    public static FindReplaceOperation FromXml(XElement element) =>
        new(
            element.Attribute("type")?.Value ?? "FindNext",
            element.Attribute("direction")?.Value ?? "Forward",
            element.Attribute("MatchWholeWords")?.Value == "True",
            element.Attribute("MatchCase")?.Value == "True",
            element.Attribute("WithinOptions")?.Value ?? "All",
            element.Attribute("AcrossOptions")?.Value ?? "All");

    public static FindReplaceOperation Default() =>
        new("FindNext", "Forward", true, true, "All", "All");
}
