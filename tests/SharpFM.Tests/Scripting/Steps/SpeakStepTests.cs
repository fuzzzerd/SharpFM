using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SpeakStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="66" name="Speak"><Calculation><![CDATA["text_to_speak"]]></Calculation><SpeechOptions WaitForCompletion="True" VoiceName="Daria" VoiceId="2769835375" VoiceCreator="4242" /></Step>
        """;

    private const string WithoutOptionsXml = """
        <Step enable="True" id="66" name="Speak"><Calculation><![CDATA["hello"]]></Calculation></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SpeakStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_WithoutOptions_IsPreserved()
    {
        var source = XElement.Parse(WithoutOptionsXml);
        var step = SpeakStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsTextOnly()
    {
        var step = (SpeakStep)SpeakStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Speak [ \"text_to_speak\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Speak", out var metadata));
        Assert.Equal(66, metadata!.Id);
    }
}
