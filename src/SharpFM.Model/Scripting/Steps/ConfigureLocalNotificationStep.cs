using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    private ConfigureLocalNotificationStep() : base(false)
    {
        Children = new StepChildBag();
    }

    public ConfigureLocalNotificationStep(StepChildBag? children = null, bool enabled = true)
        : base(enabled)
    {
        Children = children ?? new StepChildBag();
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConfigureLocalNotificationStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ConfigureLocalNotificationStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Shape =
        [
            new Passthrough { PocoProperty = "Children" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
