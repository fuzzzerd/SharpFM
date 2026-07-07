using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 34;
    public const string XmlName = "Close File";

    public FileReference? File { get; set; }

    private CloseFileStep() : base(false) { }

    public CloseFileStep(FileReference? file = null, bool enabled = true)
        : base(enabled)
    {
        File = file;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: the token comes from the FileReference value type
    // (absence means "Current File"), which the shape-driven renderer
    // cannot surface.
    public override string ToDisplayLine() =>
        File is null
            ? "Close File [ Current File ]"
            : $"Close File [ {File.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CloseFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FileReference? file = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Current File", System.StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(t)) continue;
            file = FileReference.FromDisplayToken(t);
            break;
        }
        return new CloseFileStep(file, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-file.html",
        // FileReference is emitted only when a specific file is configured;
        // its absence means "Current File".
        Shape = [new ValueTypeChild("FileReference") { PocoProperty = "File", Optional = true }],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
