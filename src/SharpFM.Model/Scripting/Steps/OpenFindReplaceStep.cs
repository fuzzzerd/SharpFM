using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenFindReplaceStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenFindReplaceStep : ScriptStep, IStepFactory
{
    public const int XmlId = 129;
    public const string XmlName = "Open Find/Replace";

    private OpenFindReplaceStep() : base(false) { }

    public OpenFindReplaceStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OpenFindReplaceStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] _) =>
        new OpenFindReplaceStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-find-replace.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
