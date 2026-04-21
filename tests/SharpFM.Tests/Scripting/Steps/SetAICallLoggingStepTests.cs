using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetAICallLoggingStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="217" name="Set AI Call Logging"><Set state="True" /><LLMDebugLog><FileName><Calculation><![CDATA["ai_log.txt"]]></Calculation></FileName><VerboseMode /><TruncateEmbeddingVectorsMode /></LLMDebugLog></Step>
        """;

    private const string OffXml = """
        <Step enable="True" id="217" name="Set AI Call Logging"><Set state="False" /><LLMDebugLog></LLMDebugLog></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SetAICallLoggingStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void FlagElements_PresenceMeansOn()
    {
        var step = (SetAICallLoggingStep)SetAICallLoggingStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.True(step.Logging);
        Assert.True(step.Verbose);
        Assert.True(step.TruncateMessages);
    }

    [Fact]
    public void FlagElements_AbsenceMeansOff()
    {
        var step = (SetAICallLoggingStep)SetAICallLoggingStep.Metadata.FromXml!(XElement.Parse(OffXml));
        Assert.False(step.Logging);
        Assert.False(step.Verbose);
        Assert.False(step.TruncateMessages);
    }

    [Fact]
    public void Display_EmitsAllFlags()
    {
        var step = (SetAICallLoggingStep)SetAICallLoggingStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Set AI Call Logging [ On ; Filename: \"ai_log.txt\" ; Verbose: On ; Truncate Messages: On ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set AI Call Logging", out var metadata));
        Assert.Equal(217, metadata!.Id);
    }
}
