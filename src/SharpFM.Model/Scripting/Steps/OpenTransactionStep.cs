using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

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
    public bool RestoreState { get; set; }

    private OpenTransactionStep() : base(false) { }

    public OpenTransactionStep(
        bool skipAutoEnterOptions = false,
        bool skipDataEntryValidation = false,
        bool overrideESSLockingConflicts = false,
        bool restoreState = false,
        bool enabled = true)
        : base(enabled)
    {
        SkipAutoEnterOptions = skipAutoEnterOptions;
        SkipDataEntryValidation = skipDataEntryValidation;
        OverrideESSLockingConflicts = overrideESSLockingConflicts;
        RestoreState = restoreState;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Open Transaction [ "
        + "Skip auto-enter options: " + (SkipAutoEnterOptions ? "On" : "Off")
        + " ; Skip data entry validation: " + (SkipDataEntryValidation ? "On" : "Off")
        + " ; Override ESS locking conflicts: " + (OverrideESSLockingConflicts ? "On" : "Off")
        + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OpenTransactionStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool a = ParseLabeled(tokens, "Skip auto-enter options:");
        bool b = ParseLabeled(tokens, "Skip data entry validation:");
        bool c = ParseLabeled(tokens, "Override ESS locking conflicts:");
        return new OpenTransactionStep(a, b, c, enabled: enabled);
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
        // Canonical order: Option, ESSForceCommit, SkipAutoEntry, Restore.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "SkipDataEntryValidation", HrLabel = "Skip data entry validation", Display = DisplayMode.Augmented },
            new BoolStateChild("ESSForceCommit") { PocoProperty = "OverrideESSLockingConflicts", HrLabel = "Override ESS locking conflicts", Display = DisplayMode.Augmented },
            new BoolStateChild("SkipAutoEntry") { PocoProperty = "SkipAutoEnterOptions", HrLabel = "Skip auto-enter options", Display = DisplayMode.Augmented },
            new BoolStateChild("Restore") { PocoProperty = "RestoreState", Display = DisplayMode.Hidden },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
