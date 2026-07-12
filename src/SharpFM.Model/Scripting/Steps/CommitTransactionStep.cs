using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for CommitTransactionStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class CommitTransactionStep : ScriptStep<CommitTransactionStep>, IStepFactory
{
    public const int XmlId = 206;
    public const string XmlName = "Commit Transaction";

    private CommitTransactionStep() : base(false) { }

    public CommitTransactionStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/commit-transaction.html",
    };
}
