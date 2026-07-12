using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class DeleteAccountStep : ScriptStep<DeleteAccountStep>, IStepFactory
{
    public const int XmlId = 135;
    public const string XmlName = "Delete Account";

    public Calculation AccountName { get; set; } = new("");

    private DeleteAccountStep() : base(false) { }

    public DeleteAccountStep(
        Calculation? accountName = null,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-account.html",
        // The AccountName wrapper is omitted by the unconfigured form (Optional).
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
