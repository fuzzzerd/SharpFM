using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformFindByNaturalLanguageStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="221" name="Perform Find by Natural Language"><Query><Calculation><![CDATA["find all active customers"]]></Calculation></Query></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformFindByNaturalLanguageStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Find by Natural Language", out var metadata));
        Assert.Equal(221, metadata!.Id);
    }
}
