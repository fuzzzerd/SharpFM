using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SortRecordsStep : ScriptStep<SortRecordsStep>, IStepFactory
{
    public const int XmlId = 39;
    public const string XmlName = "Sort Records";

    public bool WithDialog { get; set; }
    public bool RestoreStoredOrder { get; set; }
    public SortList? Sort { get; set; }

    /// <summary>
    /// Display edits are anchor-preserved when a stored sort order is
    /// present — the display line carries only the dialog and Restore flags,
    /// never the <c>&lt;SortList&gt;</c>.
    /// </summary>
    public override bool IsFullyEditable => Sort is null;

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    private SortRecordsStep() : base(false) { }

    public SortRecordsStep(
        bool withDialog = false,
        bool restoreStoredOrder = true,
        SortList? sort = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        RestoreStoredOrder = restoreStoredOrder;
        Sort = sort;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/sort-records.html",
        // NoInteract (inverse of WithDialog), Restore, then the SortList only
        // when a sort order is stored.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", DisplayInverted = true },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredOrder", HrLabel = "Restore" },
            new HrOnly("Restore") { Boolean = true },
            new ValueTypeChild("SortList") { PocoProperty = "Sort" },
        ],
    };
}
