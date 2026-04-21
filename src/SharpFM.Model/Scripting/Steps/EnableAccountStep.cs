using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class EnableAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 137;
    public const string XmlName = "Enable Account";

    public Calculation AccountName { get; set; }
    public string AccountOperation { get; set; }

    public EnableAccountStep(
        Calculation? accountName = null,
        string accountOperation = "Activate",
        bool enabled = true)
        : base(null, enabled)
    {
        AccountName = accountName ?? new Calculation("");
        AccountOperation = accountOperation;
    }

    private static readonly IReadOnlyDictionary<string, string> _AccountOperationToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Activate"] = "Activate",
        ["Deactivate"] = "Deactivate",
    };
    private static readonly IReadOnlyDictionary<string, string> _AccountOperationFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Activate"] = "Activate",
        ["Deactivate"] = "Deactivate",
    };
    private static string AccountOperationHr(string x) => _AccountOperationToHr.TryGetValue(x, out var h) ? h : x;
    private static string AccountOperationXml(string h) => _AccountOperationFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("AccountOperation", new XAttribute("value", AccountOperation)));

    public override string ToDisplayLine() =>
        "Enable Account [ " + "Account Name: " + AccountName.Text + " ; " + AccountOperationHr(AccountOperation) + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var accountName_vWrapEl = step.Element("AccountName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        var accountOperation_v = step.Element("AccountOperation")?.Attribute("value")?.Value ?? "Activate";
        return new EnableAccountStep(accountName_v, accountOperation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        string accountOperation_v = "Activate";
        return new EnableAccountStep(accountName_v, accountOperation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enable-account.html",
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
                Name = "AccountOperation",
                XmlElement = "AccountOperation",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["Activate", "Deactivate"],
                DefaultValue = "Activate",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
