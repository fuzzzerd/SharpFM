using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Open PDF starts a multi-step PDF from an existing file. The
/// <c>&lt;Option&gt;</c> toggle pairs with the optional
/// <c>&lt;UniversalPathList&gt;</c>, and the <c>&lt;OpenPDFFile&gt;</c>
/// wrapper carries the save type plus an optional open-password calculation.
/// </summary>
public sealed class OpenPdfStep : ScriptStep<OpenPdfStep>, IStepFactory
{
    public const int XmlId = 246;
    public const string XmlName = "Open PDF";

    public bool SpecifyFile { get; set; }
    public string Path { get; set; } = "";
    public string SaveType { get; set; } = "File";
    public Calculation? OpenPassword { get; set; }

    private OpenPdfStep() : base(false) { }

    public OpenPdfStep(
        bool specifyFile = false,
        string path = "",
        string saveType = "File",
        Calculation? openPassword = null,
        bool enabled = true)
        : base(enabled)
    {
        SpecifyFile = specifyFile;
        Path = path;
        SaveType = saveType;
        OpenPassword = openPassword;
    }

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: an open-password calculation or a non-default save
    /// type (the display shows only the path).
    /// </summary>
    public override bool IsFullyEditable => OpenPassword is null && SaveType == "File";

    // Hand-written: derives the SpecifyFile toggle from the path's presence,
    // a coupling the shape parser cannot express for a display-hidden slot.
    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        string path = "";
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!string.IsNullOrWhiteSpace(t)) { path = t; break; }
        }
        SpecifyFile = !string.IsNullOrEmpty(path);
        Path = path;
        SaveType = "File";
        OpenPassword = null;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-pdf.html",
        // Canonical: Option, the optional UniversalPathList, then the
        // always-present OpenPDFFile wrapper holding the save type and the
        // optional open-password calc.
        Shape =
        [
            // The specify-file toggle never shows: the display line carries the
            // bare path alone (its presence implies the toggle).
            new BoolStateChild("Option") { PocoProperty = "SpecifyFile", Display = DisplayMode.Hidden },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true, Display = DisplayMode.Native },
            new WrapperChild("OpenPDFFile",
            [
                new NamedTextChild("PDFSaveType") { PocoProperty = "SaveType", Required = true, DefaultValue = "File", Display = DisplayMode.Hidden },
                new NamedCalcChild("OpenPassword") { PocoProperty = "OpenPassword", Optional = true, Display = DisplayMode.Hidden },
            ]),
            // The wrapper's save type / password surface as one opaque display
            // slot, mirroring the legacy OpenPDFFile param.
            new HrOnly("OpenPDFFile"),
        ],
    };
}
