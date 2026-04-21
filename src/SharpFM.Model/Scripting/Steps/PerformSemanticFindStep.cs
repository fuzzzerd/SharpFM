using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformSemanticFindStep : ScriptStep, IStepFactory
{
    public const int XmlId = 218;
    public const string XmlName = "Perform Semantic Find";

    public StepChildBag Children { get; set; }

    public PerformSemanticFindStep(StepChildBag? children = null, bool enabled = true)
        : base(null, enabled)
    {
        Children = children ?? new StepChildBag();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        Children.AppendTo(step);
        return step;
    }

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        new PerformSemanticFindStep(StepChildBag.FromParent(step), step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new PerformSemanticFindStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
