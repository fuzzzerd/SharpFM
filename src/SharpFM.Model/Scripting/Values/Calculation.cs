using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// A FileMaker calculation expression, serialized as the CDATA body of a
/// <c>&lt;Calculation&gt;</c> element (or a caller-supplied wrapper element
/// name). The text is stored verbatim — SharpFM does not parse FM's calc
/// syntax, it only passes the expression through.
/// </summary>
public sealed record Calculation(string Text)
{
    /// <summary>
    /// Emit as <c>&lt;{elementName}&gt;&lt;![CDATA[{Text}]]&gt;&lt;/{elementName}&gt;</c>.
    /// Defaults to the <c>Calculation</c> element name, which is the most
    /// common case in FileMaker script XML.
    /// </summary>
    public XElement ToXml(string elementName = "Calculation") =>
        XElement.Parse($"<{elementName}><![CDATA[{Text}]]></{elementName}>");

    /// <summary>
    /// Parse the text body of the element (CDATA is transparent to
    /// <see cref="XElement.Value"/>). Empty and missing elements yield an
    /// empty <see cref="Calculation"/> rather than null — callers handle
    /// optional calculations by checking for presence themselves.
    /// </summary>
    public static Calculation FromXml(XElement element) => new(element.Value);
}
