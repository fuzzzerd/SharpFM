using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 196;
    public const string XmlName = "Close Data File";

    public Calculation? FileID { get; set; }

    private CloseDataFileStep() : base(false) { }

    public CloseDataFileStep(
        Calculation? fileID = null,
        bool enabled = true)
        : base(enabled)
    {
        FileID = fileID;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CloseDataFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<CloseDataFileStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-data-file.html",
        // Canonical unconfigured form is empty: the bare File ID calc is omitted when blank.
        Shape =
        [
            new BareCalcChild { PocoProperty = "FileID", HrLabel = "File ID", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
