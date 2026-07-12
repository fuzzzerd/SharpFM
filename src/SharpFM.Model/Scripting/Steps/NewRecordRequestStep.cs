using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for NewRecordRequestStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class NewRecordRequestStep : ScriptStep<NewRecordRequestStep>, IStepFactory
{
    public const int XmlId = 7;
    public const string XmlName = "New Record/Request";

    private NewRecordRequestStep() : base(false) { }

    public NewRecordRequestStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/new-record-request.html",
    };
}
