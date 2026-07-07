using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformSqlQueryByNaturalLanguageStep : ScriptStep, IStepFactory
{
    public const int XmlId = 214;
    public const string XmlName = "Perform SQL Query by Natural Language";

    public StepChildBag Children { get; set; }

    private PerformSqlQueryByNaturalLanguageStep() : base(false)
    {
        Children = new StepChildBag();
    }

    public PerformSqlQueryByNaturalLanguageStep(StepChildBag? children = null, bool enabled = true)
        : base(enabled)
    {
        Children = children ?? new StepChildBag();
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformSqlQueryByNaturalLanguageStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new PerformSqlQueryByNaturalLanguageStep(enabled: enabled);

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
