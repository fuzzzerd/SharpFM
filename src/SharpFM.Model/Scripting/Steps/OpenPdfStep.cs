using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Open PDF starts a multi-step PDF from an existing file. The
/// <c>&lt;Option&gt;</c> toggle pairs with the optional
/// <c>&lt;UniversalPathList&gt;</c>, and the <c>&lt;OpenPDFFile&gt;</c>
/// wrapper carries the save type plus an optional open-password calculation.
/// </summary>
public sealed class OpenPdfStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        string.IsNullOrEmpty(Path) ? "Open PDF" : $"Open PDF [ {Path} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OpenPdfStep>(step, Metadata);

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: an open-password calculation or a non-default save
    /// type (the display shows only the path).
    /// </summary>
    public override bool IsFullyEditable => OpenPassword is null && SaveType == "File";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "";
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!string.IsNullOrWhiteSpace(t)) { path = t; break; }
        }
        return new OpenPdfStep(!string.IsNullOrEmpty(path), path, "File", null, enabled);
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
            new BoolStateChild("Option") { PocoProperty = "SpecifyFile", Display = DisplayMode.Native },
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
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
