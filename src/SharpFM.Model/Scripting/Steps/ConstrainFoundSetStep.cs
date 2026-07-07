using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConstrainFoundSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 126;
    public const string XmlName = "Constrain Found Set";

    public bool WithoutIndexes { get; set; }
    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    private ConstrainFoundSetStep() : base(false) { RestoreStoredRequests = true; }

    public ConstrainFoundSetStep(
        bool withoutIndexes = true,
        bool restoreStoredRequests = true,
        FindRequestList? query = null,
        bool enabled = true)
        : base(enabled)
    {
        WithoutIndexes = withoutIndexes;
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConstrainFoundSetStep>(step, Metadata);

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: a stored <c>&lt;Query&gt;</c> request list, or an
    /// <c>&lt;Option&gt;</c> flag set to True (the display shows only the
    /// Restore toggle).
    /// </summary>
    public override bool IsFullyEditable => !WithoutIndexes && Query is null;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ConstrainFoundSetStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/constrain-found-set.html",
        // Option and Restore always emitted; <Query> only when a stored
        // request list is present.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "WithoutIndexes", Display = DisplayMode.Hidden },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredRequests", HrLabel = "Restore" },
            new ValueTypeChild("Query") { PocoProperty = "Query", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
