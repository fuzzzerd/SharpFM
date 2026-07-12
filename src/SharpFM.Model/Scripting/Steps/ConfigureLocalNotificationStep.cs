using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Configure Local Notification carries 13 heterogeneous parameters
/// (enum/namedCalc/script mix) whose shape is still evolving across FM
/// versions. The POCO preserves the full child set via
/// <see cref="StepChildBag"/> for lossless round-trip.
/// </summary>
public sealed class ConfigureLocalNotificationStep : ScriptStep<ConfigureLocalNotificationStep>, IStepFactory
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

    /// <summary>
    /// Display edits are anchor-preserved when a configured child subtree is
    /// present — the display line carries only the step name.
    /// </summary>
    public override bool IsFullyEditable => Children.Children.Count == 0;

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Shape =
        [
            new Passthrough { PocoProperty = "Children" },
        ],
    };
}
