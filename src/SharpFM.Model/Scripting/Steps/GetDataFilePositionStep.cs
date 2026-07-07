using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetDataFilePositionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 194;
    public const string XmlName = "Get Data File Position";

    public Calculation? FileId { get; set; }
    public FieldRef? Target { get; set; }

    private GetDataFilePositionStep() : base(false) { }

    public GetDataFilePositionStep(Calculation? fileId = null, FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        FileId = fileId;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        Target is null
            ? $"Get Data File Position [ File ID: {FileId?.Text ?? ""} ]"
            : $"Get Data File Position [ File ID: {FileId?.Text ?? ""} ; Target: {Target.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GetDataFilePositionStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation? fileId = null;
        FieldRef? target = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase))
            {
                fileId = new Calculation(t.Substring(8).Trim());
            }
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
            {
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            }
        }
        return new GetDataFilePositionStep(fileId, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-data-file-position.html",
        // Canonical unconfigured form is empty: the bare File ID calc and target are omitted when absent.
        Shape =
        [
            new BareCalcChild { PocoProperty = "FileId", HrLabel = "File ID", Required = true, Optional = true },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
