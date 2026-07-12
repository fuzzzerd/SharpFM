using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigureRegressionModelStep : ScriptStep<ConfigureRegressionModelStep>, IStepFactory
{
    public const int XmlId = 222;
    public const string XmlName = "Configure Regression Model";

    public StepChildBag Children { get; set; }

    private ConfigureRegressionModelStep() : base(false)
    {
        Children = new StepChildBag();
    }

    public ConfigureRegressionModelStep(StepChildBag? children = null, bool enabled = true)
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
        Category = "artificial intelligence",
        Shape =
        [
            new Passthrough { PocoProperty = "Children" },
        ],
    };
}
