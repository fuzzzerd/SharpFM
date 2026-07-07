using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ResetAccountPasswordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 136;
    public const string XmlName = "Reset Account Password";

    public Calculation AccountName { get; set; } = new("");
    public Calculation Password { get; set; } = new("");
    public bool ExpirePassword { get; set; }

    /// <summary><c>&lt;ChgPwdOnNextLogin value&gt;</c> as the True/False string the shape emits. Bound by the shape.</summary>
    public string ExpirePasswordValue { get => ExpirePassword ? "True" : "False"; set => ExpirePassword = value == "True"; }

    private ResetAccountPasswordStep() : base(false) { }

    public ResetAccountPasswordStep(
        Calculation? accountName = null,
        Calculation? password = null,
        bool expirePassword = false,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
        Password = password ?? new Calculation("");
        ExpirePassword = expirePassword;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Reset Account Password [ " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ; " + "Expire password: " + (ExpirePassword ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ResetAccountPasswordStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        bool expirePassword_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Expire password:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(16).Trim(); expirePassword_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new ResetAccountPasswordStep(accountName_v, password_v, expirePassword_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/reset-account-password.html",
        // Canonical: AccountName and Password (omitted when empty, Optional) then
        // the always-present ChgPwdOnNextLogin value flag.
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Native },
            new EnumValueChild("ChgPwdOnNextLogin") { PocoProperty = "ExpirePasswordValue", HrLabel = "Expire password", DefaultValue = "False", DisplayValues = ["On", "Off"], Display = DisplayMode.Augmented },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
