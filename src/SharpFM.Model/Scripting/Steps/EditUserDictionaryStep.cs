using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EditUserDictionaryStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class EditUserDictionaryStep : ScriptStep<EditUserDictionaryStep>, IStepFactory
{
    public const int XmlId = 109;
    public const string XmlName = "Edit User Dictionary";

    private EditUserDictionaryStep() : base(false) { }

    public EditUserDictionaryStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/edit-user-dictionary.html",
    };
}
