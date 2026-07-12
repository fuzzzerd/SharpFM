using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for GoToNextFieldStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class GoToNextFieldStep : ScriptStep<GoToNextFieldStep>, IStepFactory
{
    public const int XmlId = 4;
    public const string XmlName = "Go to Next Field";

    private GoToNextFieldStep() : base(false) { }

    public GoToNextFieldStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-next-field.html",
    };
}
