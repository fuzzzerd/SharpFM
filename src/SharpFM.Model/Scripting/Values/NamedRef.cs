using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// An id+name pair matching the <c>&lt;Layout id="81" name="Projects"/&gt;</c>,
/// <c>&lt;Script id="1" name="Init"/&gt;</c> and similar shapes FileMaker uses
/// for named references throughout its script XML.
///
/// The id is preserved losslessly on round-trip, fixing the historical
/// behaviour of the old generic param pipeline which hardcoded id="0".
/// </summary>
public sealed record NamedRef(int Id, string Name)
{
    /// <summary>
    /// Emit as <c>&lt;{elementName} id="{Id}" name="{Name}"/&gt;</c>. The
    /// element name varies by step (Layout, Script, TableOccurrence, etc.).
    /// </summary>
    public XElement ToXml(string elementName) =>
        new(elementName,
            new XAttribute("id", Id),
            new XAttribute("name", Name));

    /// <summary>
    /// Parse from an element matching the id+name shape. Missing or
    /// unparseable id defaults to 0 (matching FileMaker's own fallback for
    /// unresolved references).
    /// </summary>
    public static NamedRef FromXml(XElement element)
    {
        var idStr = element.Attribute("id")?.Value;
        var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
        var name = element.Attribute("name")?.Value ?? "";
        return new NamedRef(id, name);
    }
}
