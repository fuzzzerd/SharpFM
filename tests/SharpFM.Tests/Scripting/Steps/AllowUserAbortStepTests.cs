using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AllowUserAbortStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="85" name="Allow User Abort"><Set state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="85" name="Allow User Abort"><Set state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = AllowUserAbortStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = AllowUserAbortStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((AllowUserAbortStep)AllowUserAbortStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Allow User Abort [ On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((AllowUserAbortStep)AllowUserAbortStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Allow User Abort [ Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Allow User Abort", out var metadata));
        Assert.Equal(85, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
