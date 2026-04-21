using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for CheckRecordStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class CheckRecordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 19;
    public const string XmlName = "Check Record";

    public CheckRecordStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        new CheckRecordStep(step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] _) =>
        new CheckRecordStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/check-record.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
