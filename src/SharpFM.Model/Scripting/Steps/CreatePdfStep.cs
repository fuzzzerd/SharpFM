using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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
    public PdfDocument Document { get; set; } = new(null, null, null, null, true, null);
    public PdfSecurity Security { get; set; } = PdfSecurity.Default();
    public PdfView View { get; set; } = PdfView.Default();

    private CreatePdfStep() : base(false) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CreatePdfStep>(step, Metadata);

    /// <summary>
    /// The canonical Document block FileMaker writes for an unconfigured
    /// step: all pages numbered from 1 with a 1–1 page range.
    /// </summary>
    internal static PdfDocument DefaultDocument() =>
        new(null, null, null, null, true,
            new Calculation("1"), new Calculation("1"), new Calculation("1"));

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: a stored-label calculation, or Document/Security/View
    /// blocks configured beyond the canonical unconfigured form (the display
    /// shows only the Restore toggle).
    /// </summary>
    public override bool IsFullyEditable =>
        StoredLabel is null
        && Document == DefaultDocument()
        && Security == PdfSecurity.Default()
        && View == PdfView.Default();

    // Hand-written: must materialize the canonical Document block
    // (DefaultDocument) that the shape parser's plain ctor cannot supply.
    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var restore = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new CreatePdfStep(restore, null, DefaultDocument(), null, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/create-pdf.html",
        // Restore always emitted; a bare stored-label <Calculation> only when
        // set; the <CreatePDFFile> wrapper always emitted with its
        // Document / Security / View blocks.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOptions", HrLabel = "Restore" },
            new BareCalcChild { PocoProperty = "StoredLabel", Optional = true, Display = DisplayMode.Hidden },
            new WrapperChild("CreatePDFFile",
            [
                new ValueTypeChild("Document") { PocoProperty = "Document", Display = DisplayMode.Hidden },
                new ValueTypeChild("Security") { PocoProperty = "Security", Display = DisplayMode.Hidden },
                new ValueTypeChild("View") { PocoProperty = "View", Display = DisplayMode.Hidden },
            ]),
            // The wrapper's Document/Security/View blocks surface as one opaque
            // display slot, mirroring the legacy CreatePDFFile param.
            new HrOnly("CreatePDFFile"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
