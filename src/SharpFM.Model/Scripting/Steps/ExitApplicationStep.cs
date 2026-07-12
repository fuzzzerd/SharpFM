using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ExitApplicationStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ExitApplicationStep : ScriptStep<ExitApplicationStep>, IStepFactory
{
    public const int XmlId = 44;
    public const string XmlName = "Exit Application";

    private ExitApplicationStep() : base(false) { }

    public ExitApplicationStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/exit-application.html",
    };
}
