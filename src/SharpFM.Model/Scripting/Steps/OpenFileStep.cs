using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenFileStep : ScriptStep<OpenFileStep>, IStepFactory
{
    public const int XmlId = 33;
    public const string XmlName = "Open File";

    public bool OpenHidden { get; set; }
    public FileReference? File { get; set; }

    private OpenFileStep() : base(false) { }

    public OpenFileStep(bool openHidden = false, FileReference? file = null, bool enabled = true)
        : base(enabled)
    {
        OpenHidden = openHidden;
        File = file;
    }

    public override string ToDisplayLine()
    {
        var hidden = "Open hidden: " + (OpenHidden ? "On" : "Off");
        return File is null
            ? $"Open File [ {hidden} ]"
            : $"Open File [ {hidden} ; {File.ToDisplayString()} ]";
    }

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        bool hidden = false;
        FileReference? file = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Open hidden:", StringComparison.OrdinalIgnoreCase))
            {
                hidden = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                file = FileReference.FromDisplayToken(t);
            }
        }
        OpenHidden = hidden;
        File = file;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-file.html",
        // Canonical: the Option (open hidden) toggle, then the optional
        // FileReference which owns its own id/name/UniversalPathList shape.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "OpenHidden", HrLabel = "Open hidden", Display = DisplayMode.Augmented },
            new ValueTypeChild("FileReference") { PocoProperty = "File", Optional = true, Display = DisplayMode.Native },
        ],
        Notes = new StepNotes
        {
            Behavioral = "Opens a FileMaker file or reestablishes the link to an ODBC data source. Script steps after Open File execute in the file containing the script, not the opened file.",
            Constraints = "Cannot open a file from an unauthorized (unlinked) file.",
            Platform = new StepPlatformNotes
            {
                WebDirect = "Not supported.",
                Server = "Not supported.",
            },
        },
    };
}
