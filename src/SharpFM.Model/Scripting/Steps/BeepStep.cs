using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed POCO for FileMaker's "Beep" script step. Zero parameters, no
/// hidden state — the canonical minimal shape and the reference
/// implementation for the big-bang POCO migration pattern.
///
/// <para>Zero-loss audit: the step's only XML state is the three
/// attributes on <c>&lt;Step&gt;</c> (<c>enable</c>, <c>id</c>,
/// <c>name</c>). All three are round-tripped exactly. No child elements
/// exist in FM Pro's clipboard output. No advanced-syntax extensions are
/// required; the step's display text is literally its name.</para>
/// </summary>
public sealed class BeepStep : ScriptStep, IStepFactory
{
    public const int XmlId = 93;
    public const string XmlName = "Beep";

    public BeepStep(bool enabled = true) : base(null, enabled) { }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        new BeepStep(step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] _) =>
        new BeepStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/beep.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
