using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Close PDF finalises a multi-step PDF and carries the post-close behaviour
/// flags (<c>CreateDirectories</c> / <c>AutoOpen</c> / <c>CreateEmail</c>),
/// an optional output path, and a <c>&lt;ClosePDFFile&gt;</c> wrapper holding
/// the save type.
/// </summary>
public sealed class ClosePdfStep : ScriptStep<ClosePdfStep>, IStepFactory
{
    public const int XmlId = 245;
    public const string XmlName = "Close PDF";

    public bool CreateDirectories { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public string Path { get; set; } = "";
    public string SaveType { get; set; } = "File";

    private ClosePdfStep() : base(false) { }

    public ClosePdfStep(
        bool createDirectories = false,
        bool autoOpen = false,
        bool createEmail = false,
        string path = "",
        string saveType = "File",
        bool enabled = true)
        : base(enabled)
    {
        CreateDirectories = createDirectories;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        SaveType = saveType;
    }

    // Hand-written: AutoOpen/CreateEmail render as conditional bare tokens,
    // a form a BoolStateChild cannot produce (it always renders On/Off).
    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Path)) parts.Add(Path);
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        return parts.Count == 0
            ? "Close PDF"
            : $"Close PDF [ {string.Join(" ; ", parts)} ]";
    }

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        bool autoOpen = false, createEmail = false;
        string path = "";
        bool pathSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Automatically open", System.StringComparison.OrdinalIgnoreCase)) autoOpen = true;
            else if (t.Equals("Create email", System.StringComparison.OrdinalIgnoreCase)) createEmail = true;
            else if (!pathSeen && !string.IsNullOrWhiteSpace(t)) { path = t; pathSeen = true; }
        }
        CreateDirectories = false;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        Path = path;
        SaveType = "File";
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-pdf.html",
        // Three state flags always emitted; <UniversalPathList> only when a
        // path is set; the <ClosePDFFile> wrapper always emitted with its
        // save type.
        Shape =
        [
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateDirectories", Display = DisplayMode.Native },
            new BoolStateChild("AutoOpen") { PocoProperty = "AutoOpen" },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail" },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true },
            new WrapperChild("ClosePDFFile",
            [
                new NamedTextChild("PDFSaveType") { PocoProperty = "SaveType", DefaultValue = "File", Display = DisplayMode.Hidden },
            ]),
            // The wrapper's save type surfaces as one opaque display slot,
            // mirroring the legacy ClosePDFFile param.
            new HrOnly("ClosePDFFile"),
        ],
    };
}
