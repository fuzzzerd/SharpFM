using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Append PDF adds pages to an existing PDF in a multi-step assembly. The
/// <c>&lt;Option&gt;</c> toggle pairs with the optional
/// <c>&lt;UniversalPathList&gt;</c>, and the <c>&lt;AppendPDFFile&gt;</c>
/// wrapper carries the save type plus an optional open-password calculation.
/// </summary>
public sealed class AppendPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 244;
    public const string XmlName = "Append PDF";

    public bool SpecifyFile { get; set; }
    public string Path { get; set; } = "";
    public string SaveType { get; set; } = "File";
    public Calculation? OpenPassword { get; set; }

    private AppendPdfStep() : base(false) { }

    public AppendPdfStep(
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
        string.IsNullOrEmpty(Path) ? "Append PDF" : $"Append PDF [ {Path} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<AppendPdfStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "";
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!string.IsNullOrWhiteSpace(t)) { path = t; break; }
        }
        return new AppendPdfStep(!string.IsNullOrEmpty(path), path, "File", null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/append-pdf.html",
        // Option always emitted; <UniversalPathList> only when a path is set;
        // the <AppendPDFFile> wrapper always emitted with its save type and an
        // optional open-password calculation.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "SpecifyFile", Display = DisplayMode.Native },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new WrapperChild("AppendPDFFile",
            [
                new NamedTextChild("PDFSaveType") { PocoProperty = "SaveType", DefaultValue = "File", Display = DisplayMode.Hidden },
                new NamedCalcChild("OpenPassword") { PocoProperty = "OpenPassword", Optional = true, Display = DisplayMode.Hidden },
            ]),
            // The wrapper's save type / password surface as one opaque display
            // slot, mirroring the legacy AppendPDFFile param.
            new HrOnly("AppendPDFFile"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
