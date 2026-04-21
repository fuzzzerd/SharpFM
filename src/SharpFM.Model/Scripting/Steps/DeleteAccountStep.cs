using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class DeleteAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 135;
    public const string XmlName = "Delete Account";

    public Calculation AccountName { get; set; }

    public DeleteAccountStep(
        Calculation? accountName = null,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AccountName", AccountName.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Delete Account [ " + "Account Name: " + AccountName.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var accountName_vWrapEl = step.Element("AccountName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        return new DeleteAccountStep(accountName_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        return new DeleteAccountStep(accountName_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-account.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Account Name",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
