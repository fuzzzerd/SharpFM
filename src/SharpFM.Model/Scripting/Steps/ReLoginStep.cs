using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ReLoginStep : ScriptStep, IStepFactory
{
    public const int XmlId = 138;
    public const string XmlName = "Re-Login";

    public bool WithDialog { get; set; }
    public Calculation AccountName { get; set; }
    public Calculation Password { get; set; }

    public ReLoginStep(
        bool withDialog = true,
        Calculation? accountName = null,
        Calculation? password = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        AccountName = accountName ?? new Calculation("");
        Password = password ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("Password", Password.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Re-Login [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var accountName_vWrapEl = step.Element("AccountName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        var password_vWrapEl = step.Element("Password");
        var password_vCalcEl = password_vWrapEl?.Element("Calculation");
        var password_v = password_vCalcEl is not null ? Calculation.FromXml(password_vCalcEl) : new Calculation("");
        return new ReLoginStep(withDialog_v, accountName_v, password_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        return new ReLoginStep(withDialog_v, accountName_v, password_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/re-login.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
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
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
