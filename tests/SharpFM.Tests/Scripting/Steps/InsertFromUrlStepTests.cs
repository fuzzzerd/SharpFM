using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertFromUrlStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="160" name="Insert from URL"><NoInteract state="True" /><DontEncodeURL state="False" /><SelectAll state="True" /><VerifySSLCertificates state="True" /><CURLOptions><Calculation><![CDATA["--flags"]]></Calculation></CURLOptions><Calculation><![CDATA[$url]]></Calculation><Text /><Field>$file</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertFromUrlStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert from URL", out var metadata));
        Assert.Equal(160, metadata!.Id);
    }
}
