using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for CheckRecordStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class CheckRecordStep : ScriptStep<CheckRecordStep>, IStepFactory
{
    public const int XmlId = 19;
    public const string XmlName = "Check Record";

    private CheckRecordStep() : base(false) { }

    public CheckRecordStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/check-record.html",
    };
}
