using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Set Selection [ field ; Start Position: calc ; End Position: calc ].
/// The two position calcs are wrapped in <c>&lt;StartPosition&gt;</c> and
/// <c>&lt;EndPosition&gt;</c> elements containing a <c>&lt;Calculation&gt;</c>.
/// </summary>
public sealed class SetSelectionStep : ScriptStep<SetSelectionStep>, IStepFactory
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
            new FieldChild("Field") { PocoProperty = "Field", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("StartPosition") { PocoProperty = "StartPosition", HrLabel = "Start Position", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("EndPosition") { PocoProperty = "EndPosition", HrLabel = "End Position", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
