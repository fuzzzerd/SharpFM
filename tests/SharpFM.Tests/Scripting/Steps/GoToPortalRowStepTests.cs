using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GoToPortalRowStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="99" name="Go to Portal Row"><NoInteract state="True" /><SelectAll state="False" /><RowPageLocation value="ByCalculation" /><Calculation><![CDATA[1]]></Calculation></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GoToPortalRowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Go to Portal Row", out var metadata));
        Assert.Equal(99, metadata!.Id);
    }
}
