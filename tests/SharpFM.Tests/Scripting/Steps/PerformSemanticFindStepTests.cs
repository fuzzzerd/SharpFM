using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformSemanticFindStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="218" name="Perform Semantic Find"><Query><Calculation><![CDATA["search term"]]></Calculation></Query><Field table="Data" id="1" name="vec" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformSemanticFindStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Semantic Find", out var metadata));
        Assert.Equal(218, metadata!.Id);
    }
}
