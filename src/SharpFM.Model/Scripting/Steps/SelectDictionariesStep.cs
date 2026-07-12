using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SelectDictionariesStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class SelectDictionariesStep : ScriptStep<SelectDictionariesStep>, IStepFactory
{
    public const int XmlId = 108;
    public const string XmlName = "Select Dictionaries";

    private SelectDictionariesStep() : base(false) { }

    public SelectDictionariesStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/select-dictionaries.html",
    };
}
