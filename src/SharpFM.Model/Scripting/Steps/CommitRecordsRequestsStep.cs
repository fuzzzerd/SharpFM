using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

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

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private CommitRecordsRequestsStep() : this(withDialog: true) { }

    public CommitRecordsRequestsStep(
        bool withDialog = true,
        bool skipDataEntryValidation = false,
        bool forceCommit = false,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        SkipDataEntryValidation = skipDataEntryValidation;
        ForceCommit = forceCommit;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CommitRecordsRequestsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<CommitRecordsRequestsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/commit-records-requests.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", DisplayInverted = true },
            new BoolStateChild("Option") { PocoProperty = "SkipDataEntryValidation", HrLabel = "Skip data entry validation" },
            new BoolStateChild("ESSForceCommit") { PocoProperty = "ForceCommit", HrLabel = "Force commit" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
