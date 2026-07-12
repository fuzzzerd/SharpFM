using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Field" script step.
/// FM Pro emits the source XML as <c>[Calculation, Field]</c> but renders
/// display as <c>[ Field ; Calculation ]</c>; this step honors both. An
/// unconfigured Set Field carries neither child, so both are Optional.
/// </summary>
public sealed class SetFieldStep : ScriptStep<SetFieldStep>, IStepFactory
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

    // Hand-written: the display shows bare field-then-calculation tokens, the reverse of the canonical XML order; bare tokens cannot be reordered via Native/Augmented.
    public override string ToDisplayLine() =>
        $"Set Field [ {Target?.ToDisplayString() ?? ""} ; {Expression?.Text ?? ""} ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        FieldRef? target = null;
        Calculation? expression = null;

        if (hrParams.Length >= 1 && hrParams[0].Trim().Length > 0)
            target = FieldRef.FromDisplayToken(hrParams[0]);

        if (hrParams.Length >= 2 && hrParams[1].Trim().Length > 0)
            expression = new Calculation(hrParams[1].Trim());

        Target = target;
        Expression = expression;
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
    };
}
