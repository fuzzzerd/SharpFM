using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Open Transaction. Begins a record-level transaction. Three boolean
/// flags control auto-enter, data-entry validation, and external SQL
/// lock-conflict behavior; all render as inline labeled tokens in
/// display text.
///
/// <para>
/// Zero-loss audit: the <c>&lt;Restore state="False"/&gt;</c> child that
/// appears in some upstream XML sources is <b>intentionally dropped</b>
/// — FM Pro never writes or changes it, so it carries no information
/// worth round-tripping. See <c>docs/advanced-filemaker-scripting-syntax.md</c>.
/// </para>
/// </summary>
public sealed class OpenTransactionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 205;
    public const string XmlName = "Open Transaction";

    public bool SkipAutoEnterOptions { get; set; }
    public bool SkipDataEntryValidation { get; set; }
    public bool OverrideESSLockingConflicts { get; set; }

    public OpenTransactionStep(
        bool skipAutoEnterOptions = false,
        bool skipDataEntryValidation = false,
        bool overrideESSLockingConflicts = false,
        bool enabled = true)
        : base(enabled)
    {
        SkipAutoEnterOptions = skipAutoEnterOptions;
        SkipDataEntryValidation = skipDataEntryValidation;
        OverrideESSLockingConflicts = overrideESSLockingConflicts;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SkipAutoEntry", new XAttribute("state", SkipAutoEnterOptions ? "True" : "False")),
            new XElement("Option", new XAttribute("state", SkipDataEntryValidation ? "True" : "False")),
            new XElement("ESSForceCommit", new XAttribute("state", OverrideESSLockingConflicts ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Open Transaction [ "
        + "Skip auto-enter options: " + (SkipAutoEnterOptions ? "On" : "Off")
        + " ; Skip data entry validation: " + (SkipDataEntryValidation ? "On" : "Off")
        + " ; Override ESS locking conflicts: " + (OverrideESSLockingConflicts ? "On" : "Off")
        + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var a = step.Element("SkipAutoEntry")?.Attribute("state")?.Value == "True";
        var b = step.Element("Option")?.Attribute("state")?.Value == "True";
        var c = step.Element("ESSForceCommit")?.Attribute("state")?.Value == "True";
        return new OpenTransactionStep(a, b, c, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool a = ParseLabeled(tokens, "Skip auto-enter options:");
        bool b = ParseLabeled(tokens, "Skip data entry validation:");
        bool c = ParseLabeled(tokens, "Override ESS locking conflicts:");
        return new OpenTransactionStep(a, b, c, enabled);
    }

    private static bool ParseLabeled(string[] tokens, string prefix)
    {
        foreach (var tok in tokens)
        {
            if (tok.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var v = tok.Substring(prefix.Length).Trim();
                return v.Equals("On", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Params =
        [
            new ParamMetadata
            {
                Name = "SkipAutoEntry", XmlElement = "SkipAutoEntry", Type = "boolean",
                XmlAttr = "state", HrLabel = "Skip auto-enter options",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Option", XmlElement = "Option", Type = "boolean",
                XmlAttr = "state", HrLabel = "Skip data entry validation",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "ESSForceCommit", XmlElement = "ESSForceCommit", Type = "boolean",
                XmlAttr = "state", HrLabel = "Override ESS locking conflicts",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
