using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Add Account (134). Canonical form (skill, accounts reference):
/// <c>&lt;ChgPwdOnNextLogin value="…"/&gt;</c> first, then an
/// <c>&lt;AddAccount&gt;</c> wrapper holding the text <c>&lt;AccountType&gt;</c>
/// and the optional account name / password / privilege-set fields.
/// </summary>
public sealed class AddAccountStep : ScriptStep, IStepFactory
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

    private static readonly IReadOnlyDictionary<string, string> _AuthenticateViaToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["FileMaker"] = "FileMaker",
        ["External Server"] = "External Server",
        ["Apple Account"] = "Apple Account",
        ["Amazon"] = "Amazon",
        ["Google"] = "Google",
        ["Microsoft Entra ID"] = "Microsoft Entra ID",
        ["Custom OAuth"] = "Custom OAuth",
    };
    private static readonly IReadOnlyDictionary<string, string> _AuthenticateViaFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["FileMaker"] = "FileMaker",
        ["External Server"] = "External Server",
        ["Apple Account"] = "Apple Account",
        ["Amazon"] = "Amazon",
        ["Google"] = "Google",
        ["Microsoft Entra ID"] = "Microsoft Entra ID",
        ["Custom OAuth"] = "Custom OAuth",
    };
    private static string AuthenticateViaHr(string x) => _AuthenticateViaToHr.TryGetValue(x, out var h) ? h : x;
    private static string AuthenticateViaXml(string h) => _AuthenticateViaFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Add Account [ " + "Authenticate via: " + AuthenticateViaHr(AuthenticateVia) + " ; " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ; " + "Privilege Set: " + PrivilegeSet + " ; " + "Expire password: " + (ExpirePassword ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<AddAccountStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string authenticateVia_v = "FileMaker";
        foreach (var tok in tokens) { if (tok.StartsWith("Authenticate via:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(17).Trim(); authenticateVia_v = AuthenticateViaXml(v); break; } }
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        string privilegeSet_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Privilege Set:", StringComparison.OrdinalIgnoreCase)) { privilegeSet_v = tok.Substring(14).Trim(); break; } }
        bool expirePassword_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Expire password:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(16).Trim(); expirePassword_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new AddAccountStep(authenticateVia_v, accountName_v, password_v, privilegeSet_v, expirePassword_v, enabled);
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
                new NamedTextChild("AccountType") { PocoProperty = "AuthenticateVia", HrLabel = "Authenticate via", DefaultValue = "FileMaker", DisplayValues = ["FileMaker", "External Server", "Apple Account", "Amazon", "Google", "Microsoft Entra ID", "Custom OAuth"], Display = DisplayMode.Augmented },
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Augmented },
                new NamedTextChild("PrivilegeSet") { PocoProperty = "PrivilegeSet", HrLabel = "Privilege Set", Optional = true, Display = DisplayMode.Augmented },
            ]),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
