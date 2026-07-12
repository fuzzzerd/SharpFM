using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class EnterFindModeStep : ScriptStep<EnterFindModeStep>, IStepFactory
{
    public const int XmlId = 22;
    public const string XmlName = "Enter Find Mode";

    public bool Pause { get; set; }
    public bool RestoreStoredRequests { get; set; }
    public FindRequestList? Query { get; set; }

    private EnterFindModeStep() : base(false) { }

    public EnterFindModeStep(
        bool pause = true,
        bool restoreStoredRequests = true,
        FindRequestList? query = null,
        bool enabled = true)
        : base(enabled)
    {
        Pause = pause;
        RestoreStoredRequests = restoreStoredRequests;
        Query = query;
    }

    // Hand-written: bare display text defaults Pause/Restore to On (FileMaker's
    // new-step defaults), which the shape parser's false-initialized POCO
    // cannot reproduce.
    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        bool pause = true, restore = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Pause:", System.StringComparison.OrdinalIgnoreCase))
                pause = t.Substring(6).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        Pause = pause;
        RestoreStoredRequests = restore;
        Query = null;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enter-find-mode.html",
        // Pause and Restore always emitted; <Query> only when a stored
        // request list is present.
        Shape =
        [
            new BoolStateChild("Pause") { PocoProperty = "Pause", HrLabel = "Pause" },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredRequests", HrLabel = "Restore" },
            new ValueTypeChild("Query") { PocoProperty = "Query", Optional = true, Display = DisplayMode.Hidden },
            new HrOnly("Pause") { Boolean = true },
            new HrOnly("Restore") { Boolean = true },
            new HrOnly("Query"),
        ],
    };
}
