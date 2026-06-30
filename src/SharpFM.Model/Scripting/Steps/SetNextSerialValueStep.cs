using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetNextSerialValueStep : ScriptStep, IStepFactory
{
    public const int XmlId = 116;
    public const string XmlName = "Set Next Serial Value";

    // Nullable so the unconfigured form (no Field) omits the optional <Field> node.
    public FieldRef? Field { get; set; }
    public Calculation NextValue { get; set; } = new("");

    private SetNextSerialValueStep() : base(false) { }

    public SetNextSerialValueStep(FieldRef? field = null, Calculation? nextValue = null, bool enabled = true)
        : base(enabled)
    {
        Field = field;
        NextValue = nextValue ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Set Next Serial Value [ {(Field is null ? "" : Field.ToDisplayString())} ; {NextValue.Text} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetNextSerialValueStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FieldRef field = FieldRef.ForField("", 0, "");
        Calculation calc = new("");
        if (hrParams.Length >= 1) field = FieldRef.FromDisplayToken(hrParams[0].Trim());
        if (hrParams.Length >= 2) calc = new Calculation(hrParams[1].Trim());
        return new SetNextSerialValueStep(field, calc, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-next-serial-value.html",
        // Both Field and the bare next-value Calculation are omitted by the
        // unconfigured form (Optional).
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "Field", Optional = true, Display = DisplayMode.Native },
            new BareCalcChild { PocoProperty = "NextValue", HrLabel = "Next value", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Field", Required = true },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "Next value", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
