using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class TriggerClarisConnectFlowStepTests
{
    // Canonical step id is 211 (skill, control reference); was defaulted to 0.
    private const string CanonicalXml = """
        <Step enable="True" id="211" name="Trigger Claris Connect Flow"><NoInteract state="True" /><DontEncodeURL state="False" /><SelectAll state="True" /><VerifySSLCertificates state="False" /><Flow>https://flow.example.com</Flow><CURLOptions><Calculation><![CDATA["--request POST"]]></Calculation></CURLOptions><Text>$response</Text></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = TriggerClarisConnectFlowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Trigger Claris Connect Flow", out _));
    }
}
