using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using SharpFM.Tests.CanonicalSkill;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class StepXmlRendererTests
{
    [Fact]
    public void Renders_SetVariable_ToCanonicalForm()
    {
        var shape = new ShapeNode[]
        {
            new NamedCalcChild("Value") { PocoProperty = "Value", Required = true },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition" },
            new NamedTextChild("Name") { PocoProperty = "Name", Required = true },
        };
        var meta = SetVariableStep.Metadata with { Shape = shape };
        var step = new SetVariableStep(true, "$variable_name",
            new Calculation("expression"), new Calculation("1"));

        var emitted = StepXmlRenderer.Render(step, meta);
        var canonical = CanonicalSkillFixtures.Load("141-SetVariable");

        Assert.True(StructuralXml.Equal(canonical, emitted, out var why), why);
    }

    // Synthetic step exercising bool/enum/calc primitives and optional omission.
    private sealed class SampleStep : ScriptStep
    {
        public bool Restore { get; set; }
        public string FlushType { get; set; } = "Always";
        public Calculation Condition { get; set; } = new("");
        public string Note { get; set; } = "";
        public SampleStep() : base(true) { }
        public override XElement ToXml() => throw new System.NotImplementedException();
        public override string ToDisplayLine() => "";
    }

    private static StepMetadata Meta(params ShapeNode[] shape) =>
        new() { Name = "Sample", Id = 999, Category = "test", Shape = shape };

    [Fact]
    public void Emits_BoolState_And_Enum()
    {
        var step = new SampleStep { Restore = false, FlushType = "Always" };
        var xml = StepXmlRenderer.Render(step, Meta(
            new BoolStateChild("Restore"),
            new EnumValueChild("FlushType")));

        Assert.Equal("False", xml.Element("Restore")!.Attribute("state")!.Value);
        Assert.Equal("Always", xml.Element("FlushType")!.Attribute("value")!.Value);
    }

    [Fact]
    public void OptionalBareCalc_OmittedWhenEmpty_EmittedWhenSet()
    {
        var empty = StepXmlRenderer.Render(new SampleStep { Condition = new Calculation("") },
            Meta(new BareCalcChild { PocoProperty = "Condition", Optional = true }));
        Assert.Null(empty.Element("Calculation"));

        var set = StepXmlRenderer.Render(new SampleStep { Condition = new Calculation("x > 1") },
            Meta(new BareCalcChild { PocoProperty = "Condition", Optional = true }));
        Assert.Equal("x > 1", set.Element("Calculation")!.Value);
    }

    [Fact]
    public void OptionalNamedText_OmittedWhenEmpty()
    {
        var xml = StepXmlRenderer.Render(new SampleStep { Note = "" },
            Meta(new NamedTextChild("Note") { Optional = true }));
        Assert.Null(xml.Element("Note"));
    }

    [Fact]
    public void StepAttributes_AreEmitted()
    {
        var xml = StepXmlRenderer.Render(new SampleStep(), Meta());
        Assert.Equal("True", xml.Attribute("enable")!.Value);
        Assert.Equal("999", xml.Attribute("id")!.Value);
        Assert.Equal("Sample", xml.Attribute("name")!.Value);
    }
}
