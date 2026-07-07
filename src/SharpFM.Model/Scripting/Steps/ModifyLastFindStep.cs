using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ModifyLastFindStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class ModifyLastFindStep : ScriptStep, IStepFactory
{
    public const int XmlId = 24;
    public const string XmlName = "Modify Last Find";

    private ModifyLastFindStep() : base(false) { }

    public ModifyLastFindStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ModifyLastFindStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ModifyLastFindStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/modify-last-find.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
