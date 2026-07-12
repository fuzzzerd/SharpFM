using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenManageDataSourcesStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenManageDataSourcesStep : ScriptStep<OpenManageDataSourcesStep>, IStepFactory
{
    public const int XmlId = 140;
    public const string XmlName = "Open Manage Data Sources";

    private OpenManageDataSourcesStep() : base(false) { }

    public OpenManageDataSourcesStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-manage-data-sources.html",
    };
}
