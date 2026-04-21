using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ResetAccountPasswordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 136;
    public const string XmlName = "Reset Account Password";

    public Calculation AccountName { get; set; }
    public Calculation Password { get; set; }
    public bool ExpirePassword { get; set; }

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

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("Password", Password.ToXml("Calculation")),
            new XElement("ChgPwdOnNextLogin", new XAttribute("value", ExpirePassword ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Reset Account Password [ " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ; " + "Expire password: " + (ExpirePassword ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var accountName_vWrapEl = step.Element("AccountName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        var password_vWrapEl = step.Element("Password");
        var password_vCalcEl = password_vWrapEl?.Element("Calculation");
        var password_v = password_vCalcEl is not null ? Calculation.FromXml(password_vCalcEl) : new Calculation("");
        var expirePassword_v = step.Element("ChgPwdOnNextLogin")?.Attribute("value")?.Value == "True";
        return new ResetAccountPasswordStep(accountName_v, password_v, expirePassword_v, enabled);
    }

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
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Account Name",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Password",
            },
            new ParamMetadata
            {
                Name = "ChgPwdOnNextLogin",
                XmlElement = "ChgPwdOnNextLogin",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Expire password",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
