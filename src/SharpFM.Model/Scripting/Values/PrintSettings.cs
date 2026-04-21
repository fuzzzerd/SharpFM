using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// The <c>&lt;PrintSettings&gt;</c> sub-element for the Print step.
/// Known attributes are modeled explicitly; the platform-specific
/// PMPageFormat / PMPrintSettings blobs live in the opaque
/// <see cref="PlatformData"/> children.
/// </summary>
public sealed record PrintSettings(
    string PageNumberingOffset,
    bool PrintToFile,
    bool AllPages,
    bool Collated,
    string NumCopies,
    string PrintType,
    IReadOnlyList<PlatformData> PlatformData)
{
    public XElement ToXml()
    {
        var el = new XElement("PrintSettings",
            new XAttribute("PageNumberingOffset", PageNumberingOffset),
            new XAttribute("PrintToFile", PrintToFile ? "True" : "False"),
            new XAttribute("AllPages", AllPages ? "True" : "False"),
            new XAttribute("collated", Collated ? "True" : "False"),
            new XAttribute("NumCopies", NumCopies),
            new XAttribute("PrintType", PrintType));
        foreach (var pd in PlatformData) el.Add(pd.ToXml());
        return el;
    }

    public static PrintSettings FromXml(XElement element) =>
        new(
            element.Attribute("PageNumberingOffset")?.Value ?? "0",
            element.Attribute("PrintToFile")?.Value == "True",
            element.Attribute("AllPages")?.Value == "True",
            element.Attribute("collated")?.Value == "True",
            element.Attribute("NumCopies")?.Value ?? "1",
            element.Attribute("PrintType")?.Value ?? "BrowsedRecords",
            Values.PlatformData.ListFromParent(element));
}

/// <summary>
/// The <c>&lt;PageFormat&gt;</c> sub-element for the Print Setup step.
/// Orientation, scale, and paper geometry are modeled; platform blobs
/// pass through opaquely in <see cref="PlatformData"/>.
/// </summary>
public sealed record PageFormat(
    string PageOrientation,
    string ScaleFactor,
    string PrintableHeight,
    string PrintableWidth,
    string PaperRight,
    string PaperBottom,
    string PaperLeft,
    string PaperTop,
    IReadOnlyList<PlatformData> PlatformData)
{
    public XElement ToXml()
    {
        var el = new XElement("PageFormat",
            new XAttribute("PageOrientation", PageOrientation),
            new XAttribute("ScaleFactor", ScaleFactor),
            new XAttribute("PrintableHeight", PrintableHeight),
            new XAttribute("PrintableWidth", PrintableWidth),
            new XAttribute("PaperRight", PaperRight),
            new XAttribute("PaperBottom", PaperBottom),
            new XAttribute("PaperLeft", PaperLeft),
            new XAttribute("PaperTop", PaperTop));
        foreach (var pd in PlatformData) el.Add(pd.ToXml());
        return el;
    }

    public static PageFormat FromXml(XElement element) =>
        new(
            element.Attribute("PageOrientation")?.Value ?? "Portrait",
            element.Attribute("ScaleFactor")?.Value ?? "1",
            element.Attribute("PrintableHeight")?.Value ?? "734",
            element.Attribute("PrintableWidth")?.Value ?? "576",
            element.Attribute("PaperRight")?.Value ?? "594",
            element.Attribute("PaperBottom")?.Value ?? "774",
            element.Attribute("PaperLeft")?.Value ?? "-18",
            element.Attribute("PaperTop")?.Value ?? "-18",
            Values.PlatformData.ListFromParent(element));
}
