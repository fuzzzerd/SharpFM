using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CutStep : ScriptStep, IStepFactory
{
    public const int XmlId = 46;
    public const string XmlName = "Cut";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private CutStep() : base(false) { Select = true; }

    public CutStep(
        bool select = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CutStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<CutStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/cut.html",
        // Canonical 046-Cut: only <SelectAll>; <Field> is omitted until a
        // target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
