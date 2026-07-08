using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetFileExistsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 188;
    public const string XmlName = "Get File Exists";

    public string Path { get; set; }
    public FieldRef? Target { get; set; }

    private GetFileExistsStep() : base(false) { Path = ""; }

    public GetFileExistsStep(string path = "", FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GetFileExistsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<GetFileExistsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-file-exists.html",
        // Canonical unconfigured form is empty: path and target are omitted when absent.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Required = true, Optional = true, DisplayEmptyAs = "" },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
