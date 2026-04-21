using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class EnterBrowseModeStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="55" name="Enter Browse Mode"><Pause state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="55" name="Enter Browse Mode"><Pause state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = EnterBrowseModeStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = EnterBrowseModeStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((EnterBrowseModeStep)EnterBrowseModeStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Enter Browse Mode [ Pause: On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((EnterBrowseModeStep)EnterBrowseModeStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Enter Browse Mode [ Pause: Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Enter Browse Mode", out var metadata));
        Assert.Equal(55, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
