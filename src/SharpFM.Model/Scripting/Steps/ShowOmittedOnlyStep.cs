using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowOmittedOnlyStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ShowOmittedOnlyStep : ScriptStep<ShowOmittedOnlyStep>, IStepFactory
{
    public const int XmlId = 27;
    public const string XmlName = "Show Omitted Only";

    private ShowOmittedOnlyStep() : base(false) { }

    public ShowOmittedOnlyStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-omitted-only.html",
    };
}
