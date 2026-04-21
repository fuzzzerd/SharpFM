using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class EnterPreviewModeStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="41" name="Enter Preview Mode"><Pause state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="41" name="Enter Preview Mode"><Pause state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = EnterPreviewModeStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = EnterPreviewModeStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((EnterPreviewModeStep)EnterPreviewModeStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Enter Preview Mode [ Pause: On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((EnterPreviewModeStep)EnterPreviewModeStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Enter Preview Mode [ Pause: Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Enter Preview Mode", out var metadata));
        Assert.Equal(41, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
