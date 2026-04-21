using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ChangePasswordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 83;
    public const string XmlName = "Change Password";

    public Calculation OldPassword { get; set; }
    public Calculation Password { get; set; }
    public bool WithDialog { get; set; }

    public ChangePasswordStep(
        Calculation? oldPassword = null,
        Calculation? password = null,
        bool withDialog = false,
        bool enabled = true)
        : base(enabled)
    {
        OldPassword = oldPassword ?? new Calculation("");
        Password = password ?? new Calculation("");
        WithDialog = withDialog;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("OldPassword", OldPassword.ToXml("Calculation")),
            new XElement("NewPassword", Password.ToXml("Calculation")),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")));

    public override string ToDisplayLine() =>
        "Change Password [ " + "Old Password: " + OldPassword.Text + " ; " + "Password: " + Password.Text + " ; " + "With dialog: " + (WithDialog ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var oldPassword_vWrapEl = step.Element("OldPassword");
        var oldPassword_vCalcEl = oldPassword_vWrapEl?.Element("Calculation");
        var oldPassword_v = oldPassword_vCalcEl is not null ? Calculation.FromXml(oldPassword_vCalcEl) : new Calculation("");
        var password_vWrapEl = step.Element("NewPassword");
        var password_vCalcEl = password_vWrapEl?.Element("Calculation");
        var password_v = password_vCalcEl is not null ? Calculation.FromXml(password_vCalcEl) : new Calculation("");
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        return new ChangePasswordStep(oldPassword_v, password_v, withDialog_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? oldPassword_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Old Password:", StringComparison.OrdinalIgnoreCase)) { oldPassword_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        bool withDialog_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new ChangePasswordStep(oldPassword_v, password_v, withDialog_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/change-password.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Old Password",
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
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
