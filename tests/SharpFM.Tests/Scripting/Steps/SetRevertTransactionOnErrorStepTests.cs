using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetRevertTransactionOnErrorStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="223" name="Set Revert Transaction on Error"><Set state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="223" name="Set Revert Transaction on Error"><Set state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = SetRevertTransactionOnErrorStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = SetRevertTransactionOnErrorStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "On"
        // (boolean: XML True displays as On).
        var stepTrue = ((SetRevertTransactionOnErrorStep)SetRevertTransactionOnErrorStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Set Revert Transaction on Error [ Revert on error: On ]", stepTrue.ToDisplayLine());

        var stepFalse = ((SetRevertTransactionOnErrorStep)SetRevertTransactionOnErrorStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Set Revert Transaction on Error [ Revert on error: Off ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Revert Transaction on Error", out var metadata));
        Assert.Equal(223, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
