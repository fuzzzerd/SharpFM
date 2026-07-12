using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToFieldStep : ScriptStep<GoToFieldStep>, IStepFactory
{
    public const int XmlId = 17;
    public const string XmlName = "Go to Field";

    public bool SelectPerform { get; set; }
    public FieldRef? Target { get; set; }

    private GoToFieldStep() : base(false) { SelectPerform = true; }

    public GoToFieldStep(
        bool selectPerform = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectPerform = selectPerform;
        Target = target;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-field.html",
        // Canonical 017-GoToField: unconfigured form is <SelectAll> only;
        // the configured variant (-2) adds <Field>, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "SelectPerform", HrLabel = "Select/perform", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
