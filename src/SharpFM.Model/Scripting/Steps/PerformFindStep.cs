using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformFindStep : ScriptStep, IStepFactory
{
    public const int XmlId = 28;
    public const string XmlName = "Perform Find";

    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    /// <summary>
    /// Display edits are anchor-preserved when stored find requests are
    /// present — the display line carries only the Restore flag, never the
    /// <c>&lt;Query&gt;</c> request list.
    /// </summary>
    public override bool IsFullyEditable => Query is null;

    private PerformFindStep() : base(false) { }

    public PerformFindStep(bool restoreStoredRequests = true, FindRequestList? query = null, bool enabled = true)
        : base(enabled)
    {
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformFindStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<PerformFindStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-find.html",
        // Canonical order: always-emitted <Restore state=.../>, then the
        // <Query> request list only when the step carries stored requests.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredRequests", HrLabel = "Restore", Display = DisplayMode.Native },
            new ValueTypeChild("Query") { PocoProperty = "Query", Optional = true, Display = DisplayMode.Hidden },
            new HrOnly("Restore") { Boolean = true },
            new HrOnly("Query"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
