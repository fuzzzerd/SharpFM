using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Configure Local Notification carries 13 heterogeneous parameters
/// (enum/namedCalc/script mix) whose shape is still evolving across FM
/// versions. The POCO preserves the full child set via
/// <see cref="StepChildBag"/> for lossless round-trip.
/// </summary>
public sealed class ConfigureLocalNotificationStep : ScriptStep, IStepFactory
{
    public const int XmlId = 187;
    public const string XmlName = "Configure Local Notification";

    public StepChildBag Children { get; set; }

    public ConfigureLocalNotificationStep(StepChildBag? children = null, bool enabled = true)
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
        new ConfigureLocalNotificationStep(StepChildBag.FromParent(step), step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ConfigureLocalNotificationStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
