using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenRecordRequestStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenRecordRequestStep : ScriptStep<OpenRecordRequestStep>, IStepFactory
{
    public const int XmlId = 133;
    public const string XmlName = "Open Record/Request";

    private OpenRecordRequestStep() : base(false) { }

    public OpenRecordRequestStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-record-request.html",
    };
}
