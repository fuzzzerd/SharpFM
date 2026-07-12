using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for CopyAllRecordsRequestsStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class CopyAllRecordsRequestsStep : ScriptStep<CopyAllRecordsRequestsStep>, IStepFactory
{
    public const int XmlId = 98;
    public const string XmlName = "Copy All Records/Requests";

    private CopyAllRecordsRequestsStep() : base(false) { }

    public CopyAllRecordsRequestsStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/copy-all-records-requests.html",
    };
}
