using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Field" script step.
/// FM Pro emits the source XML as <c>[Calculation, Field]</c> but renders
/// display as <c>[ Field ; Calculation ]</c>; this step honors both. An
/// unconfigured Set Field carries neither child, so both are Optional.
/// </summary>
public sealed class SetFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 76;
    public const string XmlName = "Set Field";

    public FieldRef? Target { get; set; }
    public Calculation? Expression { get; set; }

    private SetFieldStep() : base(false) { }

    public SetFieldStep(bool enabled, FieldRef? target, Calculation? expression)
        : base(enabled)
    {
        Target = target;
        Expression = expression;
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetFieldStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Set Field [ {Target?.ToDisplayString() ?? ""} ; {Expression?.Text ?? ""} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var target = FieldRef.ForField(null, 0, "");
        var expression = new Calculation("");

        if (hrParams.Length >= 1)
            target = FieldRef.FromDisplayToken(hrParams[0]);

        if (hrParams.Length >= 2)
            expression = new Calculation(hrParams[1].Trim());

        return new SetFieldStep(enabled, target, expression);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-field.html",
        // Canonical 076-SetField: source order is bare <Calculation> then
        // <Field>. The unconfigured variant (-1) has neither, so both Optional.
        Shape =
        [
            new BareCalcChild { PocoProperty = "Expression", Optional = true },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
