using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// PDF document metadata block nested under PDFOptions. All four title
/// fields are optional calculations.
/// </summary>
public sealed record PdfDocument(
    Calculation? Title,
    Calculation? Subject,
    Calculation? Author,
    Calculation? Keywords,
    bool AllPages,
    Calculation? NumberFrom)
{
    public XElement ToXml()
    {
        var el = new XElement("Document");
        if (Title is not null) el.Add(new XElement("Title", Title.ToXml("Calculation")));
        if (Subject is not null) el.Add(new XElement("Subject", Subject.ToXml("Calculation")));
        if (Author is not null) el.Add(new XElement("Author", Author.ToXml("Calculation")));
        if (Keywords is not null) el.Add(new XElement("Keywords", Keywords.ToXml("Calculation")));
        var pages = new XElement("Pages", new XAttribute("AllPages", AllPages ? "True" : "False"));
        if (NumberFrom is not null) pages.Add(new XElement("NumberFrom", NumberFrom.ToXml("Calculation")));
        el.Add(pages);
        return el;
    }

    public static PdfDocument FromXml(XElement element)
    {
        static Calculation? ReadCalc(XElement? parent) =>
            parent?.Element("Calculation") is { } c ? Calculation.FromXml(c) : null;

        var pages = element.Element("Pages");
        return new PdfDocument(
            ReadCalc(element.Element("Title")),
            ReadCalc(element.Element("Subject")),
            ReadCalc(element.Element("Author")),
            ReadCalc(element.Element("Keywords")),
            pages?.Attribute("AllPages")?.Value == "True",
            ReadCalc(pages?.Element("NumberFrom")));
    }
}

/// <summary>Security attributes for PDF export.</summary>
public sealed record PdfSecurity(
    bool AllowScreenReader,
    bool EnableCopying,
    string ControlEditing,
    string ControlPrinting,
    bool RequireControlEditPassword,
    bool RequireOpenPassword)
{
    public XElement ToXml() =>
        new("Security",
            new XAttribute("allowScreenReader", AllowScreenReader ? "True" : "False"),
            new XAttribute("enableCopying", EnableCopying ? "True" : "False"),
            new XAttribute("controlEditing", ControlEditing),
            new XAttribute("controlPrinting", ControlPrinting),
            new XAttribute("requireControlEditPassword", RequireControlEditPassword ? "True" : "False"),
            new XAttribute("requireOpenPassword", RequireOpenPassword ? "True" : "False"));

    public static PdfSecurity FromXml(XElement element) =>
        new(
            element.Attribute("allowScreenReader")?.Value == "True",
            element.Attribute("enableCopying")?.Value == "True",
            element.Attribute("controlEditing")?.Value ?? "AnyExceptExtractingPages",
            element.Attribute("controlPrinting")?.Value ?? "HighResolution",
            element.Attribute("requireControlEditPassword")?.Value == "True",
            element.Attribute("requireOpenPassword")?.Value == "True");

    public static PdfSecurity Default() => new(true, true, "AnyExceptExtractingPages", "HighResolution", false, false);
}

/// <summary>PDF initial-view attributes.</summary>
public sealed record PdfView(string Magnification, string PageLayout, string Show)
{
    public XElement ToXml() =>
        new("View",
            new XAttribute("magnification", Magnification),
            new XAttribute("pageLayout", PageLayout),
            new XAttribute("show", Show));

    public static PdfView FromXml(XElement element) =>
        new(
            element.Attribute("magnification")?.Value ?? "100",
            element.Attribute("pageLayout")?.Value ?? "SinglePage",
            element.Attribute("show")?.Value ?? "PagesPanelAndPage");

    public static PdfView Default() => new("100", "SinglePage", "PagesPanelAndPage");
}

/// <summary>Full PDFOptions block with document / security / view.</summary>
public sealed record PdfOptions(string Source, string? Appearance, PdfDocument Document, PdfSecurity Security, PdfView View)
{
    public XElement ToXml()
    {
        var el = new XElement("PDFOptions", new XAttribute("source", Source));
        if (Appearance is not null) el.Add(new XAttribute("appearance", Appearance));
        el.Add(Document.ToXml());
        el.Add(Security.ToXml());
        el.Add(View.ToXml());
        return el;
    }

    public static PdfOptions FromXml(XElement element)
    {
        var docEl = element.Element("Document");
        var secEl = element.Element("Security");
        var viewEl = element.Element("View");
        return new PdfOptions(
            element.Attribute("source")?.Value ?? "RecordsBeingBrowsed",
            element.Attribute("appearance")?.Value,
            docEl is not null ? PdfDocument.FromXml(docEl) : new PdfDocument(null, null, null, null, true, null),
            secEl is not null ? PdfSecurity.FromXml(secEl) : PdfSecurity.Default(),
            viewEl is not null ? PdfView.FromXml(viewEl) : PdfView.Default());
    }
}
