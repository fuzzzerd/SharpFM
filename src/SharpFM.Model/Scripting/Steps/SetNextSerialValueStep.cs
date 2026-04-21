using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetNextSerialValueStep : ScriptStep, IStepFactory
{
    public const int XmlId = 116;
    public const string XmlName = "Set Next Serial Value";

    public FieldRef Field { get; set; }
    public Calculation NextValue { get; set; }

    public SetNextSerialValueStep(FieldRef? field = null, Calculation? nextValue = null, bool enabled = true)
        : base(enabled)
    {
        Field = field ?? FieldRef.ForField("", 0, "");
        NextValue = nextValue ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            Field.ToXml("Field"),
            NextValue.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        $"Set Next Serial Value [ {Field.ToDisplayString()} ; {NextValue.Text} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        return new SetNextSerialValueStep(field, calc, enabled);
    }

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
        Params =
        [
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Field", Required = true },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "Next value", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
