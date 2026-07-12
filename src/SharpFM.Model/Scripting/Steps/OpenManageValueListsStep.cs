using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenManageValueListsStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenManageValueListsStep : ScriptStep<OpenManageValueListsStep>, IStepFactory
{
    public const int XmlId = 112;
    public const string XmlName = "Open Manage Value Lists";

    private OpenManageValueListsStep() : base(false) { }

    public OpenManageValueListsStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-manage-value-lists.html",
    };
}
