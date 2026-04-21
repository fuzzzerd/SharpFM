using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformRagActionStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="219" name="Perform RAG Action"><AccountName><Calculation><![CDATA["account"]]></Calculation></AccountName><Action value="Query" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformRagActionStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform RAG Action", out var metadata));
        Assert.Equal(219, metadata!.Id);
    }
}
