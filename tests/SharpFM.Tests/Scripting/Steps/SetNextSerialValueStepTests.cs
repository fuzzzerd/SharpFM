using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetNextSerialValueStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="116" name="Set Next Serial Value"><Field table="Customer" id="3" name="id" /><Calculation><![CDATA[Max ( id ) + 1]]></Calculation></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SetNextSerialValueStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsFieldAndNextValue()
    {
        var step = (SetNextSerialValueStep)SetNextSerialValueStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Set Next Serial Value [ Customer::id (#3) ; Max ( id ) + 1 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Next Serial Value", out var metadata));
        Assert.Equal(116, metadata!.Id);
    }
}
