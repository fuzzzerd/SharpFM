using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class DeletePortalRowStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="104" name="Delete Portal Row"><NoInteract state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="104" name="Delete Portal Row"><NoInteract state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = DeletePortalRowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = DeletePortalRowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "Off"
        // (invertedHr: XML True displays as Off).
        var stepTrue = ((DeletePortalRowStep)DeletePortalRowStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Delete Portal Row [ With dialog: Off ]", stepTrue.ToDisplayLine());

        var stepFalse = ((DeletePortalRowStep)DeletePortalRowStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Delete Portal Row [ With dialog: On ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Delete Portal Row", out var metadata));
        Assert.Equal(104, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
