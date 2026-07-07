using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class EnableAccountStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Enable Account [ " + "Account Name: " + AccountName.Text + " ; " + AccountOperationHr(AccountOperation) + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<EnableAccountStep>(step, Metadata);

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
        // AccountName (omitted when empty, Optional) then the always-present
        // AccountOperation value flag.
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native },
            new EnumValueChild("AccountOperation") { PocoProperty = "AccountOperation", DefaultValue = "Activate", DisplayValues = ["Activate", "Deactivate"], Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
