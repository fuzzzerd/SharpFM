using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformScriptOnServerWithCallbackStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="210" name="Perform Script on Server with Callback"><CallbackScriptState value="Continue" /><Calculation><![CDATA[$optional_parameter]]></Calculation><Script id="5" name="Sync" /><CallbackScript><ScriptName id="6" name="OnDone" /><ScriptParameter><Calculation><![CDATA[$optional_parameter]]></Calculation></ScriptParameter></CallbackScript></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformScriptOnServerWithCallbackStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Script on Server with Callback", out var metadata));
        Assert.Equal(210, metadata!.Id);
    }
}
