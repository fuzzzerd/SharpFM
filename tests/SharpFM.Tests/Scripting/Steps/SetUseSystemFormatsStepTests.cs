using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetUseSystemFormatsStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="94" name="Set Use System Formats"><Set state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="94" name="Set Use System Formats"><Set state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = SetUseSystemFormatsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = SetUseSystemFormatsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((SetUseSystemFormatsStep)SetUseSystemFormatsStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Set Use System Formats [ Use system formats: On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((SetUseSystemFormatsStep)SetUseSystemFormatsStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Set Use System Formats [ Use system formats: Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Use System Formats", out var metadata));
        Assert.Equal(94, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
