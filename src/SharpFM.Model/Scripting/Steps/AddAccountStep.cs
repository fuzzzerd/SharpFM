using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class AddAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 134;
    public const string XmlName = "Add Account";

    public string AuthenticateVia { get; set; }
    public Calculation AccountName { get; set; }
    public Calculation Password { get; set; }
    public string PrivilegeSet { get; set; }
    public bool ExpirePassword { get; set; }

    public AddAccountStep(
        string authenticateVia = "FileMaker",
        Calculation? accountName = null,
        Calculation? password = null,
        string privilegeSet = "",
        bool expirePassword = false,
        bool enabled = true)
        : base(null, enabled)
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

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AccountType", new XAttribute("value", AuthenticateVia)),
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("Password", Password.ToXml("Calculation")),
            new XElement("PrivilegeSet", PrivilegeSet),
            new XElement("ChgPwdOnNextLogin", new XAttribute("value", ExpirePassword ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Add Account [ " + "Authenticate via: " + AuthenticateViaHr(AuthenticateVia) + " ; " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ; " + "Privilege Set: " + PrivilegeSet + " ; " + "Expire password: " + (ExpirePassword ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var authenticateVia_v = step.Element("AccountType")?.Attribute("value")?.Value ?? "FileMaker";
        var accountName_vWrapEl = step.Element("AccountName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        var password_vWrapEl = step.Element("Password");
        var password_vCalcEl = password_vWrapEl?.Element("Calculation");
        var password_v = password_vCalcEl is not null ? Calculation.FromXml(password_vCalcEl) : new Calculation("");
        var privilegeSet_v = step.Element("PrivilegeSet")?.Value ?? "";
        var expirePassword_v = step.Element("ChgPwdOnNextLogin")?.Attribute("value")?.Value == "True";
        return new AddAccountStep(authenticateVia_v, accountName_v, password_v, privilegeSet_v, expirePassword_v, enabled);
    }

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
        Params =
        [
            new ParamMetadata
            {
                Name = "AccountType",
                XmlElement = "AccountType",
                Type = "enum",
                HrLabel = "Authenticate via",
                ValidValues = ["FileMaker", "External Server", "Apple Account", "Amazon", "Google", "Microsoft Entra ID", "Custom OAuth"],
                DefaultValue = "FileMaker",
            },
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
                Name = "PrivilegeSet",
                XmlElement = "PrivilegeSet",
                Type = "text",
                HrLabel = "Privilege Set",
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
