using System;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CheckSelectionStep : ScriptStep<CheckSelectionStep>, IStepFactory
{
    public const int XmlId = 18;
    public const string XmlName = "Check Selection";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private CheckSelectionStep() : base(false) { Select = true; }

    public CheckSelectionStep(
        bool select = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/check-selection.html",
        // Canonical 018-CheckSelection: only <SelectAll>; <Field> is omitted
        // until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
