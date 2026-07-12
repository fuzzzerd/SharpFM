using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for DuplicateRecordRequestStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class DuplicateRecordRequestStep : ScriptStep<DuplicateRecordRequestStep>, IStepFactory
{
    public const int XmlId = 8;
    public const string XmlName = "Duplicate Record/Request";

    private DuplicateRecordRequestStep() : base(false) { }

    public DuplicateRecordRequestStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/duplicate-record-request.html",
    };
}
