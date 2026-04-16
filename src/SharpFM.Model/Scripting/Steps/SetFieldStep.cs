using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Field" script step.
/// FM Pro emits the source XML as <c>[Calculation, Field]</c> but renders
/// display as <c>[ Field ; Calculation ]</c>; this step honors both.
/// </summary>
public sealed class SetFieldStep : ScriptStep
{
    public FieldRef Target { get; set; }
    public Calculation Expression { get; set; }

    public SetFieldStep(bool enabled, FieldRef target, Calculation expression)
        : base(StepCatalogLoader.ByName["Set Field"], enabled)
    {
        Target = target;
        Expression = expression;
    }

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register typed step factories on assembly load.")]
    [ModuleInitializer]
    internal static void Register()
    {
        StepXmlFactory.Register("Set Field", FromXml);
        StepDisplayFactory.Register("Set Field", FromDisplayParams);
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
            new XAttribute("id", 76),
            new XAttribute("name", "Set Field"));

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
}
