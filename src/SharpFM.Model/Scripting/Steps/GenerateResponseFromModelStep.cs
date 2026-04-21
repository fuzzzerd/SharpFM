using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GenerateResponseFromModelStep : ScriptStep, IStepFactory
{
    public const int XmlId = 220;
    public const string XmlName = "Generate Response from Model";

    public StepChildBag Children { get; set; }

    public GenerateResponseFromModelStep(StepChildBag? children = null, bool enabled = true)
        : base(enabled)
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
        new GenerateResponseFromModelStep(StepChildBag.FromParent(step), step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new GenerateResponseFromModelStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
