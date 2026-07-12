using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertCurrentUserNameStep : ScriptStep<InsertCurrentUserNameStep>, IStepFactory
{
    public const int XmlId = 60;
    public const string XmlName = "Insert Current User Name";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private InsertCurrentUserNameStep() : base(false) { Select = true; }

    public InsertCurrentUserNameStep(
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
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-current-user-name.html",
        // Canonical 060-InsertCurrentUserName: only <SelectAll>; <Field> is
        // omitted until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
