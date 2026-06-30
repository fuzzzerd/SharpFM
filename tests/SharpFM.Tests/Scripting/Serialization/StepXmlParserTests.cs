using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;
using SharpFM.Tests.CanonicalSkill;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class StepXmlParserTests
{
    // Mirrors Set Variable's shape on a step with a parameterless ctor so the
    // shape-driven parser can construct it by reflection.
    private sealed class VarLikeStep : ScriptStep
    {
        public string Name { get; set; } = "";
        public Calculation Value { get; set; } = new("");
        public Calculation Repetition { get; set; } = new("1");
        public VarLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Set Variable",
            Id = 141,
            Category = "control",
            Shape =
            [
                new NamedCalcChild("Value") { PocoProperty = "Value" },
                new NamedCalcChild("Repetition") { PocoProperty = "Repetition" },
                new NamedTextChild("Name") { PocoProperty = "Name" },
            ],
        };
    }

    [Fact]
    public void Parses_SetVariable_PopulatesProperties()
    {
        var canonical = CanonicalSkillFixtures.Load("141-SetVariable");
        var step = StepXmlParser.Parse<VarLikeStep>(canonical, VarLikeStep.Meta);

        Assert.True(step.Enabled);
        Assert.Equal("$variable_name", step.Name);
        Assert.Equal("expression", step.Value.Text);
        Assert.Equal("1", step.Repetition.Text);
    }

    [Fact]
    public void RoundTrips_SetVariable_ParseThenRender()
    {
        var canonical = CanonicalSkillFixtures.Load("141-SetVariable");
        var step = StepXmlParser.Parse<VarLikeStep>(canonical, VarLikeStep.Meta);
        var emitted = step.ToXml();

        Assert.True(StructuralXml.Equal(canonical, emitted, out var why), why);
    }

    [Fact]
    public void Parses_DisabledStep_FromEnableAttr()
    {
        var xml = XElement.Parse(
            "<Step enable=\"False\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$x</Name></Step>");
        var step = StepXmlParser.Parse<VarLikeStep>(xml, VarLikeStep.Meta);
        Assert.False(step.Enabled);
    }
}
