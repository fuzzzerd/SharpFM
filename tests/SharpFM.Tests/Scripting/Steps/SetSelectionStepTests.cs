using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetSelectionStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="130" name="Set Selection"><Field table="Notes" id="1" name="body" /><StartPosition><Calculation><![CDATA[1]]></Calculation></StartPosition><EndPosition><Calculation><![CDATA[10]]></Calculation></EndPosition></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SetSelectionStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsFieldAndPositions()
    {
        var step = (SetSelectionStep)SetSelectionStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Set Selection [ Notes::body (#1) ; Start Position: 1 ; End Position: 10 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Selection", out var metadata));
        Assert.Equal(130, metadata!.Id);
    }
}
