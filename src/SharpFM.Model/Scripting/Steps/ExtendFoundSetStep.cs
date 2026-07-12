using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExtendFoundSetStep : ScriptStep<ExtendFoundSetStep>, IStepFactory
{
    public const int XmlId = 127;
    public const string XmlName = "Extend Found Set";

    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    private ExtendFoundSetStep() : base(false) { RestoreStoredRequests = true; }

    public ExtendFoundSetStep(bool restoreStoredRequests = true, FindRequestList? query = null, bool enabled = true)
        : base(enabled)
    {
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/extend-found-set.html",
        // Restore is always emitted; Query only when stored requests exist.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredRequests", HrLabel = "Restore" },
            new ValueTypeChild("Query") { PocoProperty = "Query", Optional = true },
        ],
    };
}
