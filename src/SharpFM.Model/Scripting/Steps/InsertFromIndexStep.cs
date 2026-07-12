using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertFromIndexStep : ScriptStep<InsertFromIndexStep>, IStepFactory
{
    public const int XmlId = 11;
    public const string XmlName = "Insert from Index";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private InsertFromIndexStep() : base(false) { Select = true; }

    public InsertFromIndexStep(
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
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-index.html",
        // Canonical 011-InsertFromIndex: only <SelectAll>; <Field> is omitted
        // until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
