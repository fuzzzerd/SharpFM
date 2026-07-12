using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenManageLayoutsStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenManageLayoutsStep : ScriptStep<OpenManageLayoutsStep>, IStepFactory
{
    public const int XmlId = 151;
    public const string XmlName = "Open Manage Layouts";

    private OpenManageLayoutsStep() : base(false) { }

    public OpenManageLayoutsStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-manage-layouts.html",
    };
}
