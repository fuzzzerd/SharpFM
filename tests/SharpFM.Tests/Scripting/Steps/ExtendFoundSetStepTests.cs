using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ExtendFoundSetStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="127" name="Extend Found Set"><Restore state="True" /><Query><RequestRow operation="Include"><Criteria><Field table="Customer" id="1" name="state" /><Text>$query</Text></Criteria></RequestRow></Query></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ExtendFoundSetStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Extend Found Set", out var metadata));
        Assert.Equal(127, metadata!.Id);
    }
}
