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

    public override string ToDisplayLine() =>
        $"Create PDF [ Restore: {(RestoreStoredOptions ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CreatePdfStep>(step, Metadata);

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
        ],
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
