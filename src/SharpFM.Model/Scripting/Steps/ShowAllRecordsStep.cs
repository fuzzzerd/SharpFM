using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowAllRecordsStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ShowAllRecordsStep : ScriptStep<ShowAllRecordsStep>, IStepFactory
{
    public const int XmlId = 23;
    public const string XmlName = "Show All Records";

    private ShowAllRecordsStep() : base(false) { }

    public ShowAllRecordsStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-all-records.html",
    };
}
