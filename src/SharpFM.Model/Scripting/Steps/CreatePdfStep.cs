using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Create PDF starts a multi-step PDF assembly. It carries the
/// restore-stored-options flag, an optional stored-label calculation, and a
/// <c>&lt;CreatePDFFile&gt;</c> wrapper whose <c>Document</c> / <c>Security</c>
/// / <c>View</c> blocks reuse the same value types as Save Records as PDF.
/// </summary>
public sealed class CreatePdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 243;
    public const string XmlName = "Create PDF";

    public bool RestoreStoredOptions { get; set; }
    public Calculation? StoredLabel { get; set; }
    public PdfDocument Document { get; set; }
    public PdfSecurity Security { get; set; }
    public PdfView View { get; set; }

    public CreatePdfStep(
        bool restoreStoredOptions = false,
        Calculation? storedLabel = null,
        PdfDocument? document = null,
        PdfSecurity? security = null,
        PdfView? view = null,
        bool enabled = true)
        : base(enabled)
    {
        RestoreStoredOptions = restoreStoredOptions;
        StoredLabel = storedLabel;
        Document = document ?? new PdfDocument(null, null, null, null, true, null);
        Security = security ?? PdfSecurity.Default();
        View = view ?? PdfView.Default();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Restore", new XAttribute("state", RestoreStoredOptions ? "True" : "False")));
        if (StoredLabel is not null) step.Add(StoredLabel.ToXml("Calculation"));
        step.Add(new XElement("CreatePDFFile", Document.ToXml(), Security.ToXml(), View.ToXml()));
        return step;
    }

    public override string ToDisplayLine() =>
        $"Create PDF [ Restore: {(RestoreStoredOptions ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var labelEl = step.Element("Calculation");
        var label = labelEl is not null ? Calculation.FromXml(labelEl) : null;
        var file = step.Element("CreatePDFFile");
        var docEl = file?.Element("Document");
        var secEl = file?.Element("Security");
        var viewEl = file?.Element("View");
        var doc = docEl is not null ? PdfDocument.FromXml(docEl) : null;
        var sec = secEl is not null ? PdfSecurity.FromXml(secEl) : null;
        var view = viewEl is not null ? PdfView.FromXml(viewEl) : null;
        return new CreatePdfStep(restore, label, doc, sec, view, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var restore = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new CreatePdfStep(restore, null, null, null, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/create-pdf.html",
        Params =
        [
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
            new ParamMetadata { Name = "CreatePDFFile", XmlElement = "CreatePDFFile", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
