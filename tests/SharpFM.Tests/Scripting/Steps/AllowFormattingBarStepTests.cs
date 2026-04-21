using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AllowFormattingBarStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="115" name="Allow Formatting Bar"><Set state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="115" name="Allow Formatting Bar"><Set state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = AllowFormattingBarStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = AllowFormattingBarStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((AllowFormattingBarStep)AllowFormattingBarStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Allow Formatting Bar [ On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((AllowFormattingBarStep)AllowFormattingBarStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Allow Formatting Bar [ Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Allow Formatting Bar", out var metadata));
        Assert.Equal(115, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
