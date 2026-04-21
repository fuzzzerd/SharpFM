using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureAIAccountStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="212" name="Configure AI Account"><AccoutName><Calculation><![CDATA[$x]]></Calculation></AccoutName><LLMType value="OpenAI"/><Endpoint><Calculation><![CDATA[$x]]></Calculation></Endpoint><VerifySSLCertificates state="True"/><AccessAPIKey><Calculation><![CDATA[$x]]></Calculation></AccessAPIKey></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureAIAccountStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = ConfigureAIAccountStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = ConfigureAIAccountStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure AI Account", out var metadata));
        Assert.Equal(212, metadata!.Id);
    }
}
