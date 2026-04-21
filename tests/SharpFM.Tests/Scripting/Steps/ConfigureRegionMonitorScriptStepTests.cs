using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureRegionMonitorScriptStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="185" name="Configure Region Monitor Script"><Script id="5" name="OnRegion" /><Identifier><Calculation><![CDATA["region1"]]></Calculation></Identifier></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureRegionMonitorScriptStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Region Monitor Script", out var metadata));
        Assert.Equal(185, metadata!.Id);
    }
}
