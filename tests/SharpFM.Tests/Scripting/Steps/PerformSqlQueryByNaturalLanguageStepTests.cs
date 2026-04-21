using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformSqlQueryByNaturalLanguageStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="214" name="Perform SQL Query by Natural Language"><Query><Calculation><![CDATA["show me all customers in CA"]]></Calculation></Query></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformSqlQueryByNaturalLanguageStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform SQL Query by Natural Language", out var metadata));
        Assert.Equal(214, metadata!.Id);
    }
}
