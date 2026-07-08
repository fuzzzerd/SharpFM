using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetDataFilePositionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 195;
    public const string XmlName = "Set Data File Position";

    public Calculation? FileID { get; set; }
    public Calculation? NewPosition { get; set; }

    private SetDataFilePositionStep() : base(false) { }

    public SetDataFilePositionStep(
        Calculation? fileID = null,
        Calculation? newPosition = null,
        bool enabled = true)
        : base(enabled)
    {
        FileID = fileID;
        NewPosition = newPosition;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetDataFilePositionStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SetDataFilePositionStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-data-file-position.html",
        // Canonical: the optional bare <Calculation> (file id) and <position>
        // calculation; the unconfigured form is an empty step.
        Shape =
        [
            new BareCalcChild { PocoProperty = "FileID", HrLabel = "File ID", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("position") { PocoProperty = "NewPosition", HrLabel = "New position", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
