using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Set Selection [ field ; Start Position: calc ; End Position: calc ].
/// The two position calcs are wrapped in <c>&lt;StartPosition&gt;</c> and
/// <c>&lt;EndPosition&gt;</c> elements containing a <c>&lt;Calculation&gt;</c>.
/// </summary>
public sealed class SetSelectionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 130;
    public const string XmlName = "Set Selection";

    public FieldRef? Field { get; set; }
    public Calculation StartPosition { get; set; }
    public Calculation EndPosition { get; set; }

    public SetSelectionStep(FieldRef? field = null, Calculation? startPosition = null, Calculation? endPosition = null, bool enabled = true)
        : base(null, enabled)
    {
        Field = field;
        StartPosition = startPosition ?? new Calculation("");
        EndPosition = endPosition ?? new Calculation("");
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (Field is not null) step.Add(Field.ToXml("Field"));
        step.Add(new XElement("StartPosition", StartPosition.ToXml("Calculation")));
        step.Add(new XElement("EndPosition", EndPosition.ToXml("Calculation")));
        return step;
    }

    public override string ToDisplayLine()
    {
        var field = Field is null ? "" : $"{Field.ToDisplayString()} ; ";
        return $"Set Selection [ {field}Start Position: {StartPosition.Text} ; End Position: {EndPosition.Text} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        var startEl = step.Element("StartPosition")?.Element("Calculation");
        var start = startEl is not null ? Calculation.FromXml(startEl) : new Calculation("");
        var endEl = step.Element("EndPosition")?.Element("Calculation");
        var end = endEl is not null ? Calculation.FromXml(endEl) : new Calculation("");
        return new SetSelectionStep(field, start, end, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FieldRef? field = null;
        Calculation start = new("");
        Calculation end = new("");
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Start Position:", StringComparison.OrdinalIgnoreCase))
                start = new Calculation(t.Substring(15).Trim());
            else if (t.StartsWith("End Position:", StringComparison.OrdinalIgnoreCase))
                end = new Calculation(t.Substring(13).Trim());
            else if (!string.IsNullOrWhiteSpace(t) && field is null)
                field = FieldRef.FromDisplayToken(t);
        }
        return new SetSelectionStep(field, start, end, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-selection.html",
        Params =
        [
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Field" },
            new ParamMetadata { Name = "StartPosition", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Start Position", Required = true },
            new ParamMetadata { Name = "EndPosition", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "End Position", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
