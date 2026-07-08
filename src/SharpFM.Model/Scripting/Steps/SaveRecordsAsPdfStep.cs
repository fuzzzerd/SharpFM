using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveRecordsAsPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 144;
    public const string XmlName = "Save Records as PDF";

    public bool WithDialog { get; set; }
    public bool Append { get; set; }
    public bool CreateDirectories { get; set; }
    public bool RestoreStoredOptions { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string Path { get; set; }
    public Calculation? StoredLabel { get; set; }
    public PdfOptions? Options { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private SaveRecordsAsPdfStep() : this(enabled: true) { }

    public SaveRecordsAsPdfStep(
        bool withDialog = false,
        bool append = false,
        bool createDirectories = true,
        bool restoreStoredOptions = true,
        bool autoOpen = false,
        bool createEmail = false,
        string path = "",
        Calculation? storedLabel = null,
        PdfOptions? options = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Append = append;
        CreateDirectories = createDirectories;
        RestoreStoredOptions = restoreStoredOptions;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        StoredLabel = storedLabel;
        Options = options;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: conditional file and Append tokens plus hidden option flags are grammar the shape renderer cannot express.
    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
            Path,
        };
        if (Append) parts.Add("Append");
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        return $"Save Records as PDF [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveRecordsAsPdfStep>(step, Metadata);

    /// <summary>
    /// The canonical PDFOptions block FileMaker writes for an unconfigured
    /// step: all-pages document numbered from 1 with a 1–1 page range, and
    /// default security/view settings.
    /// </summary>
    internal static PdfOptions DefaultOptions() => new(
        "RecordsBeingBrowsed",
        null,
        new PdfDocument(null, null, null, null, true,
            new Calculation("1"), new Calculation("1"), new Calculation("1")),
        PdfSecurity.Default(),
        PdfView.Default());

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: restored stored options (and their label), a
    /// disabled create-directories flag, or a PDFOptions block configured
    /// beyond the canonical unconfigured form.
    /// </summary>
    public override bool IsFullyEditable =>
        !RestoreStoredOptions && StoredLabel is null && CreateDirectories
        && DefaultOptions().Equals(Options);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = false, append = false, autoOpen = false, createEmail = false;
        string path = "";
        bool pathSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", System.StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.Equals("Append", System.StringComparison.OrdinalIgnoreCase)) append = true;
            else if (t.Equals("Automatically open", System.StringComparison.OrdinalIgnoreCase)) autoOpen = true;
            else if (t.Equals("Create email", System.StringComparison.OrdinalIgnoreCase)) createEmail = true;
            else if (!pathSeen && !string.IsNullOrWhiteSpace(t)) { path = t; pathSeen = true; }
        }
        return new SaveRecordsAsPdfStep(withDialog, append, true, false, autoOpen, createEmail, path, null, DefaultOptions(), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-records-as-pdf.html",
        // Canonical: six state flags, then the optional path, the optional
        // stored-label calc, and the PDFOptions block which owns its own shape.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("Option") { PocoProperty = "Append", HrLabel = "Append" },
            new BoolStateChild("CreateDirectories"),
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOptions" },
            new BoolStateChild("AutoOpen"),
            new BoolStateChild("CreateEmail"),
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new BareCalcChild { PocoProperty = "StoredLabel", Optional = true },
            new ValueTypeChild("PDFOptions") { PocoProperty = "Options", Optional = true, Display = DisplayMode.Hidden },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
