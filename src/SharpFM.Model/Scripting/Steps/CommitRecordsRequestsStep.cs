using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Commit Records/Requests. Saves pending record/request changes. The
/// <c>NoInteract</c> flag is inverted in display ("With dialog: On" maps
/// to <c>state="False"</c>). Two additional flags skip data-entry
/// validation and force-commit through external SQL lock conflicts.
/// </summary>
public sealed class CommitRecordsRequestsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 75;
    public const string XmlName = "Commit Records/Requests";

    /// <summary>Show the commit confirmation dialog. Maps to inverted NoInteract.</summary>
    public bool WithDialog { get; set; }
    /// <summary>Skip data entry validation on commit.</summary>
    public bool SkipDataEntryValidation { get; set; }
    /// <summary>Override ESS/ODBC locking conflicts on commit.</summary>
    public bool ForceCommit { get; set; }

    public CommitRecordsRequestsStep(
        bool withDialog = true,
        bool skipDataEntryValidation = false,
        bool forceCommit = false,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        SkipDataEntryValidation = skipDataEntryValidation;
        ForceCommit = forceCommit;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("Option", new XAttribute("state", SkipDataEntryValidation ? "True" : "False")),
            new XElement("ESSForceCommit", new XAttribute("state", ForceCommit ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Commit Records/Requests [ "
        + "With dialog: " + (WithDialog ? "On" : "Off")
        + " ; Skip data entry validation: " + (SkipDataEntryValidation ? "On" : "Off")
        + " ; Force commit: " + (ForceCommit ? "On" : "Off")
        + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var noInteract = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var option = step.Element("Option")?.Attribute("state")?.Value == "True";
        var essForce = step.Element("ESSForceCommit")?.Attribute("state")?.Value == "True";
        return new CommitRecordsRequestsStep(
            withDialog: !noInteract,
            skipDataEntryValidation: option,
            forceCommit: essForce,
            enabled: enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        return new CommitRecordsRequestsStep(
            withDialog: ParseOn(tokens, "With dialog:", defaultValue: true),
            skipDataEntryValidation: ParseOn(tokens, "Skip data entry validation:", defaultValue: false),
            forceCommit: ParseOn(tokens, "Force commit:", defaultValue: false),
            enabled: enabled);
    }

    private static bool ParseOn(string[] tokens, string prefix, bool defaultValue)
    {
        foreach (var tok in tokens)
        {
            if (tok.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var v = tok.Substring(prefix.Length).Trim();
                return v.Equals("On", StringComparison.OrdinalIgnoreCase);
            }
        }
        return defaultValue;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/commit-records-requests.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract", XmlElement = "NoInteract", Type = "boolean",
                XmlAttr = "state", HrLabel = "With dialog",
                // Inverted: XML state=True means HR "With dialog: Off".
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
                XmlAttr = "state", HrLabel = "Force commit",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
