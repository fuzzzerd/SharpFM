using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ModifyLastFindStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ModifyLastFindStep : ScriptStep<ModifyLastFindStep>, IStepFactory
{
    public const int XmlId = 24;
    public const string XmlName = "Modify Last Find";

    private ModifyLastFindStep() : base(false) { }

    public ModifyLastFindStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/modify-last-find.html",
    };
}
