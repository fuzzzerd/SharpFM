using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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
    public Calculation StartPosition { get; set; } = new("");
    public Calculation EndPosition { get; set; } = new("");

    private SetSelectionStep() : base(false) { }

    public SetSelectionStep(FieldRef? field = null, Calculation? startPosition = null, Calculation? endPosition = null, bool enabled = true)
        : base(enabled)
    {
        Field = field;
        StartPosition = startPosition ?? new Calculation("");
        EndPosition = endPosition ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var field = Field is null ? "" : $"{Field.ToDisplayString()} ; ";
        return $"Set Selection [ {field}Start Position: {StartPosition.Text} ; End Position: {EndPosition.Text} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetSelectionStep>(step, Metadata);

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
        // Field, StartPosition and EndPosition are all omitted by the
        // unconfigured form (Optional).
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "Field", HrLabel = "Field", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("StartPosition") { PocoProperty = "StartPosition", HrLabel = "Start Position", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("EndPosition") { PocoProperty = "EndPosition", HrLabel = "End Position", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
