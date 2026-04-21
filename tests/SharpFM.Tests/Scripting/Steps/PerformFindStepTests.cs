using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformFindStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="28" name="Perform Find"><Restore state="True" /><Query><RequestRow operation="Include"><Criteria><Field table="Customer" id="1" name="state" /><Text>$query</Text></Criteria></RequestRow><RequestRow operation="Exclude"><Criteria><Field table="Customer" id="2" name="inactive" /><Text>1</Text></Criteria></RequestRow></Query></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformFindStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void MultipleRequests_ExcludeOperationIsPreserved()
    {
        var step = (PerformFindStep)PerformFindStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal(2, step.Query!.Requests.Count);
        Assert.Equal("Include", step.Query.Requests[0].Operation);
        Assert.Equal("Exclude", step.Query.Requests[1].Operation);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Find", out var metadata));
        Assert.Equal(28, metadata!.Id);
    }
}
