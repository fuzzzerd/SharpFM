using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for FlushCacheToDiskStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class FlushCacheToDiskStep : ScriptStep<FlushCacheToDiskStep>, IStepFactory
{
    public const int XmlId = 102;
    public const string XmlName = "Flush Cache to Disk";

    private FlushCacheToDiskStep() : base(false) { }

    public FlushCacheToDiskStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/flush-cache-to-disk.html",
    };
}
