using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformRagActionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 219;
    public const string XmlName = "Perform RAG Action";

    public StepChildBag Children { get; set; }

    private PerformRagActionStep() : base(false)
    {
        Children = new StepChildBag();
    }

    public PerformRagActionStep(StepChildBag? children = null, bool enabled = true)
        : base(enabled)
    {
        Children = children ?? new StepChildBag();
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => XmlName;

    /// <summary>
    /// Display edits are anchor-preserved when a configured child subtree is
    /// present — the display line carries only the step name.
    /// </summary>
    public override bool IsFullyEditable => Children.Children.Count == 0;

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformRagActionStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new PerformRagActionStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        Shape =
        [
            new Passthrough { PocoProperty = "Children" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
