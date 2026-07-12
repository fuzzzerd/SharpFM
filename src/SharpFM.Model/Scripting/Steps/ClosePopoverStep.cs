using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ClosePopoverStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ClosePopoverStep : ScriptStep<ClosePopoverStep>, IStepFactory
{
    public const int XmlId = 169;
    public const string XmlName = "Close Popover";

    private ClosePopoverStep() : base(false) { }

    public ClosePopoverStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-popover.html",
    };
}
