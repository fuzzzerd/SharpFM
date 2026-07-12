using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Add Account (134). Canonical form (skill, accounts reference):
/// <c>&lt;ChgPwdOnNextLogin value="…"/&gt;</c> first, then an
/// <c>&lt;AddAccount&gt;</c> wrapper holding the text <c>&lt;AccountType&gt;</c>
/// and the optional account name / password / privilege-set fields.
/// </summary>
public sealed class AddAccountStep : ScriptStep<AddAccountStep>, IStepFactory
{
    public const int XmlId = 134;
    public const string XmlName = "Add Account";

    public string AuthenticateVia { get; set; } = "FileMaker";
    public Calculation AccountName { get; set; } = new("");
    public Calculation Password { get; set; } = new("");
    public string PrivilegeSet { get; set; } = "";
    public bool ExpirePassword { get; set; }

    private AddAccountStep() : base(false) { }

    public AddAccountStep(
        string authenticateVia = "FileMaker",
        Calculation? accountName = null,
        Calculation? password = null,
        string privilegeSet = "",
        bool expirePassword = false,
        bool enabled = true)
        : base(enabled)
    {
        AuthenticateVia = authenticateVia;
        AccountName = accountName ?? new Calculation("");
        Password = password ?? new Calculation("");
        PrivilegeSet = privilegeSet;
        ExpirePassword = expirePassword;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/add-account.html",
        // Canonical: ChgPwdOnNextLogin (value attr) first, then the <AddAccount>
        // wrapper with text <AccountType> and the optional account fields.
        Shape =
        [
            new BoolStateChild("ChgPwdOnNextLogin", "value") { PocoProperty = "ExpirePassword", HrLabel = "Expire password", Display = DisplayMode.Augmented },
            new WrapperChild("AddAccount",
            [
                new NamedTextChild("AccountType") { PocoProperty = "AuthenticateVia", HrLabel = "Authenticate via", DefaultValue = "FileMaker", DisplayValues = ["FileMaker", "External Server", "Apple Account", "Amazon", "Google", "Microsoft Entra ID", "Custom OAuth"], Display = DisplayMode.Native },
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
                new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
                new NamedTextChild("PrivilegeSet") { PocoProperty = "PrivilegeSet", HrLabel = "Privilege Set", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            ]),
        ],
    };
}
