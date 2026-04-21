using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConstrainFoundSetStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="126" name="Constrain Found Set"><Option state="True" /><Restore state="True" /><Query><RequestRow operation="Include"><Criteria><Field table="Customer" id="1" name="state" /><Text>$query</Text></Criteria></RequestRow></Query></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConstrainFoundSetStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Query_IsTyped()
    {
        var step = (ConstrainFoundSetStep)ConstrainFoundSetStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.NotNull(step.Query);
        Assert.Single(step.Query!.Requests);
        Assert.Equal("Include", step.Query.Requests[0].Operation);
        Assert.Single(step.Query.Requests[0].Criteria);
        Assert.Equal("$query", step.Query.Requests[0].Criteria[0].Query);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Constrain Found Set", out var metadata));
        Assert.Equal(126, metadata!.Id);
    }
}
