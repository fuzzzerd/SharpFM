using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for HaltScriptStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class HaltScriptStep : ScriptStep<HaltScriptStep>, IStepFactory
{
    public const int XmlId = 90;
    public const string XmlName = "Halt Script";

    private HaltScriptStep() : base(false) { }

    public HaltScriptStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/halt-script.html",
    };
}
