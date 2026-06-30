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
    Calculation? NumberFrom,
    Calculation? PageRangeFrom = null,
    Calculation? PageRangeTo = null)
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
        if (PageRangeFrom is not null || PageRangeTo is not null)
        {
            var range = new XElement("PageRange");
            if (PageRangeFrom is not null) range.Add(new XElement("From", PageRangeFrom.ToXml("Calculation")));
            if (PageRangeTo is not null) range.Add(new XElement("To", PageRangeTo.ToXml("Calculation")));
            pages.Add(range);
        }
        el.Add(pages);
        return el;
    }

    public static PdfDocument FromXml(XElement element)
    {
        static Calculation? ReadCalc(XElement? parent) =>
            parent?.Element("Calculation") is { } c ? Calculation.FromXml(c) : null;

        var pages = element.Element("Pages");
        var range = pages?.Element("PageRange");
        return new PdfDocument(
            ReadCalc(element.Element("Title")),
            ReadCalc(element.Element("Subject")),
            ReadCalc(element.Element("Author")),
            ReadCalc(element.Element("Keywords")),
            pages?.Attribute("AllPages")?.Value == "True",
            ReadCalc(pages?.Element("NumberFrom")),
            ReadCalc(range?.Element("From")),
            ReadCalc(range?.Element("To")));
    }
}

/// <summary>Security attributes for PDF export.</summary>
public sealed record PdfSecurity(
    bool AllowScreenReader,
    bool EnableCopying,
    string ControlEditing,
    string ControlPrinting,
    bool RequireControlEditPassword,
    bool RequireOpenPassword,
    Calculation? OpenPassword = null,
    Calculation? ControlPassword = null)
{
    public XElement ToXml()
    {
        var el = new XElement("Security",
            new XAttribute("allowScreenReader", AllowScreenReader ? "True" : "False"),
            new XAttribute("enableCopying", EnableCopying ? "True" : "False"),
            new XAttribute("controlEditing", ControlEditing),
            new XAttribute("controlPrinting", ControlPrinting),
            new XAttribute("requireControlEditPassword", RequireControlEditPassword ? "True" : "False"),
            new XAttribute("requireOpenPassword", RequireOpenPassword ? "True" : "False"));
        if (OpenPassword is not null) el.Add(new XElement("OpenPassword", OpenPassword.ToXml("Calculation")));
        if (ControlPassword is not null) el.Add(new XElement("ControlPassword", ControlPassword.ToXml("Calculation")));
        return el;
    }

    public static PdfSecurity FromXml(XElement element)
    {
        static Calculation? ReadCalc(XElement? parent) =>
            parent?.Element("Calculation") is { } c ? Calculation.FromXml(c) : null;
        return new(
            element.Attribute("allowScreenReader")?.Value == "True",
            element.Attribute("enableCopying")?.Value == "True",
            element.Attribute("controlEditing")?.Value ?? "AnyExceptExtractingPages",
            element.Attribute("controlPrinting")?.Value ?? "HighResolution",
            element.Attribute("requireControlEditPassword")?.Value == "True",
            element.Attribute("requireOpenPassword")?.Value == "True",
            ReadCalc(element.Element("OpenPassword")),
            ReadCalc(element.Element("ControlPassword")));
    }

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
public sealed record PdfOptions(string Source, string? Appearance, PdfDocument Document, PdfSecurity Security, PdfView View, string SaveType = "File")
{
    public XElement ToXml()
    {
        var el = new XElement("PDFOptions", new XAttribute("source", Source));
        if (Appearance is not null) el.Add(new XAttribute("appearance", Appearance));
        // Canonical PDFOptions leads with the <PDFSaveType> element.
        el.Add(new XElement("PDFSaveType", SaveType));
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
            viewEl is not null ? PdfView.FromXml(viewEl) : PdfView.Default(),
            element.Element("PDFSaveType")?.Value ?? "File");
    }
}
