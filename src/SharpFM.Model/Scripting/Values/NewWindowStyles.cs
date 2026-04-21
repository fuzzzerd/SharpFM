using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// The <c>&lt;NewWndStyles&gt;</c> sub-element used by window-creating
/// steps (Go to List of Records, Go to Related Record, New Window). All
/// attributes round-trip losslessly; the <c>Styles</c> attribute is a
/// numeric bitmask that FM Pro uses to encode the final effective style
/// set and is preserved as a string.
/// </summary>
public sealed record NewWindowStyles(
    string Style,
    string Close,
    string Minimize,
    string Maximize,
    string Resize,
    string Styles,
    string? DimParentWindow = null,
    string? Toolbars = null,
    string? MenuBar = null)
{
    public XElement ToXml()
    {
        var el = new XElement("NewWndStyles",
            new XAttribute("Style", Style),
            new XAttribute("Close", Close),
            new XAttribute("Minimize", Minimize),
            new XAttribute("Maximize", Maximize),
            new XAttribute("Resize", Resize));
        if (DimParentWindow is not null) el.Add(new XAttribute("DimParentWindow", DimParentWindow));
        if (Toolbars is not null) el.Add(new XAttribute("Toolbars", Toolbars));
        if (MenuBar is not null) el.Add(new XAttribute("MenuBar", MenuBar));
        el.Add(new XAttribute("Styles", Styles));
        return el;
    }

    public static NewWindowStyles FromXml(XElement element) =>
        new(
            element.Attribute("Style")?.Value ?? "Document",
            element.Attribute("Close")?.Value ?? "Yes",
            element.Attribute("Minimize")?.Value ?? "Yes",
            element.Attribute("Maximize")?.Value ?? "Yes",
            element.Attribute("Resize")?.Value ?? "Yes",
            element.Attribute("Styles")?.Value ?? "0",
            element.Attribute("DimParentWindow")?.Value,
            element.Attribute("Toolbars")?.Value,
            element.Attribute("MenuBar")?.Value);

    public static NewWindowStyles Default() =>
        new("Document", "Yes", "Yes", "Yes", "Yes", "3606018");
}
