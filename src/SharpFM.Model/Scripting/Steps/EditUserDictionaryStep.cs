using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EditUserDictionaryStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class EditUserDictionaryStep : ScriptStep, IStepFactory
{
    public const int XmlId = 109;
    public const string XmlName = "Edit User Dictionary";

    private EditUserDictionaryStep() : base(false) { }

    public EditUserDictionaryStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<EditUserDictionaryStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<EditUserDictionaryStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/edit-user-dictionary.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
