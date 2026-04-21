using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SendEventStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="57" name="Send Event"><ContentType value="Text" /><Text /><Event CopyResultToClipboard="False" WaitForCompletion="True" BringTargetToForeground="False" TargetType="NUTD" TargetName="&lt;unknown&gt;" id="dosc" class="misc" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SendEventStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Send Event", out var metadata));
        Assert.Equal(57, metadata!.Id);
    }
}
