using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SaveRecordsAsJsonlStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="225" name="Save Records as JSONL"><Option state="False" /><CreateDirectories state="False" /><FineTuneFormat state="True" /><AutoOpen state="False" /><CreateEmail state="False" /><UniversalPathList>$path</UniversalPathList><SaveAsJSONL><SystemPrompt><Calculation><![CDATA["system_prompt"]]></Calculation></SystemPrompt><UserPrompt><Calculation><![CDATA["user_prompt"]]></Calculation></UserPrompt><AssistantPrompt><Calculation><![CDATA["assistant_prompt"]]></Calculation></AssistantPrompt></SaveAsJSONL></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SaveRecordsAsJsonlStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Save Records as JSONL", out var metadata));
        Assert.Equal(225, metadata!.Id);
    }
}
