using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class FineTuneModelStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="213" name="Fine-Tune Model"><AccountName><Calculation><![CDATA["account"]]></Calculation></AccountName><Model><Calculation><![CDATA["gpt-4"]]></Calculation></Model></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = FineTuneModelStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Fine-Tune Model", out var metadata));
        Assert.Equal(213, metadata!.Id);
    }
}
