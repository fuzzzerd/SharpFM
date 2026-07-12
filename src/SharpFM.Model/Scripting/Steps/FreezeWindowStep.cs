using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for FreezeWindowStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class FreezeWindowStep : ScriptStep<FreezeWindowStep>, IStepFactory
{
    public const int XmlId = 79;
    public const string XmlName = "Freeze Window";

    private FreezeWindowStep() : base(false) { }

    public FreezeWindowStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/freeze-window.html",
    };
}
