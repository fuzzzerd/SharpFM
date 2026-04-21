using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// The <c>&lt;DialogOptions&gt;</c> sub-element for Insert File. Carries
/// the dialog's asFile/enable flags and three config children: Title (a
/// named calc), Storage (Insert/Reference/UserChoice), Compress (When/Never/UserChoice),
/// and an opaque FilterList.
/// </summary>
public sealed record InsertFileDialogOptions(
    bool AsFile,
    bool Enable,
    Calculation? Title,
    string StorageType,
    string CompressType,
    string? FilterListXml)
{
    public XElement ToXml()
    {
        var el = new XElement("DialogOptions",
            new XAttribute("asFile", AsFile ? "True" : "False"),
            new XAttribute("enable", Enable ? "True" : "False"));
        if (Title is not null) el.Add(new XElement("Title", Title.ToXml("Calculation")));
        el.Add(new XElement("Storage", new XAttribute("type", StorageType)));
        el.Add(new XElement("Compress", new XAttribute("type", CompressType)));
        if (FilterListXml is not null) el.Add(XElement.Parse(FilterListXml));
        else el.Add(new XElement("FilterList"));
        return el;
    }

    public static InsertFileDialogOptions FromXml(XElement element)
    {
        var titleEl = element.Element("Title")?.Element("Calculation");
        var filterList = element.Element("FilterList");
        return new InsertFileDialogOptions(
            element.Attribute("asFile")?.Value == "True",
            element.Attribute("enable")?.Value == "True",
            titleEl is not null ? Calculation.FromXml(titleEl) : null,
            element.Element("Storage")?.Attribute("type")?.Value ?? "UserChoice",
            element.Element("Compress")?.Attribute("type")?.Value ?? "UserChoice",
            filterList is not null && filterList.HasElements ? filterList.ToString() : null);
    }
}
