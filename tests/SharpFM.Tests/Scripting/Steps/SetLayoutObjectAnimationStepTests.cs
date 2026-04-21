using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetLayoutObjectAnimationStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="168" name="Set Layout Object Animation"><Set state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="168" name="Set Layout Object Animation"><Set state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = SetLayoutObjectAnimationStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = SetLayoutObjectAnimationStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((SetLayoutObjectAnimationStep)SetLayoutObjectAnimationStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Set Layout Object Animation [ Animation: On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((SetLayoutObjectAnimationStep)SetLayoutObjectAnimationStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Set Layout Object Animation [ Animation: Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Layout Object Animation", out var metadata));
        Assert.Equal(168, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
