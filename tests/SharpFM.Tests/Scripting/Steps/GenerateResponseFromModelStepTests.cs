using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GenerateResponseFromModelStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="220" name="Generate Response from Model"><AccountName><Calculation><![CDATA["account"]]></Calculation></AccountName><Model><Calculation><![CDATA["gpt-4"]]></Calculation></Model><UserPrompt><Calculation><![CDATA["hello"]]></Calculation></UserPrompt></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GenerateResponseFromModelStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Generate Response from Model", out var metadata));
        Assert.Equal(220, metadata!.Id);
    }
}
