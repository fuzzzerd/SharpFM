using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Cancel PDF aborts an in-progress multi-step PDF assembly. It carries no
/// parameters — the canonical form is an empty <c>&lt;Step&gt;</c> element.
/// </summary>
public sealed class CancelPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 247;
    public const string XmlName = "Cancel PDF";

    private CancelPdfStep() : base(false) { }

    public CancelPdfStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => "Cancel PDF";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<CancelPdfStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new CancelPdfStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/cancel-pdf.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
