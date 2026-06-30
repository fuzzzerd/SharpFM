using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Cancel PDF aborts an in-progress multi-step PDF assembly. It carries no
/// parameters — the canonical form is an empty <c>&lt;Step&gt;</c> element.
/// </summary>
public sealed class CancelPdfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 247;
    public const string XmlName = "Cancel PDF";

    public CancelPdfStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

    public override string ToDisplayLine() => "Cancel PDF";

    public static new ScriptStep FromXml(XElement step) =>
        new CancelPdfStep(step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new CancelPdfStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/cancel-pdf.html",
        Params = [],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
