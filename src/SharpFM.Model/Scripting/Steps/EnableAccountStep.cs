using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class EnableAccountStep : ScriptStep<EnableAccountStep>, IStepFactory
{
    public const int XmlId = 137;
    public const string XmlName = "Enable Account";

    public Calculation AccountName { get; set; } = new("");
    public string AccountOperation { get; set; } = "Activate";

    private EnableAccountStep() : base(false) { }

    public EnableAccountStep(
        Calculation? accountName = null,
        string accountOperation = "Activate",
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
        AccountOperation = accountOperation;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enable-account.html",
        // AccountName (omitted when empty, Optional) then the always-present
        // AccountOperation value flag.
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new EnumValueChild("AccountOperation") { PocoProperty = "AccountOperation", DefaultValue = "Activate", DisplayValues = ["Activate", "Deactivate"], Display = DisplayMode.Native },
        ],
    };
}
