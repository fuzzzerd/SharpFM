using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Field" script step.
/// FM Pro emits the source XML as <c>[Calculation, Field]</c> but renders
/// display as <c>[ Field ; Calculation ]</c>; this step honors both.
/// </summary>
public sealed class SetFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 76;
    public const string XmlName = "Set Field";

    public FieldRef Target { get; set; }
    public Calculation Expression { get; set; }

    public SetFieldStep(bool enabled, FieldRef target, Calculation expression)
        : base(enabled)
    {
        Target = target;
        Expression = expression;
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";

        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField(null, 0, "");

        var calcEl = step.Element("Calculation");
        var expression = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");

        return new SetFieldStep(enabled, target, expression);
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

        step.Add(Expression.ToXml());
        step.Add(Target.ToXml("Field"));

        return step;
    }

    public override string ToDisplayLine() =>
        $"Set Field [ {Target.ToDisplayString()} ; {Expression.Text} ]";

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
        Params =
        [
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", Required = true },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
