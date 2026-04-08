using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// FileMaker layout transition animation. Stored as the raw wire string
/// (e.g. <c>"SlideFromLeft"</c>, <c>"CrossDissolve"</c>, <c>"None"</c>)
/// rather than a closed enum so that unknown values FileMaker may add in
/// future versions round-trip through SharpFM without loss.
///
/// <para>
/// The absence of an <c>&lt;Animation&gt;</c> element in the source XML is
/// distinct from <c>Animation("None")</c>: the former means "omit on
/// re-emission", the latter means "emit with value None". Callers use
/// nullable <see cref="Animation"/> references to carry the distinction.
/// </para>
/// </summary>
public sealed record Animation(string WireValue)
{
    public XElement ToXml() =>
        new("Animation", new XAttribute("value", WireValue));

    public static Animation FromXml(XElement element) =>
        new(element.Attribute("value")?.Value ?? "");

    /// <summary>
    /// Convenience constant for the "None" animation. Note this is NOT the
    /// same as a null Animation reference — "None" still emits the element,
    /// absence does not.
    /// </summary>
    public static Animation None => new("None");
}
