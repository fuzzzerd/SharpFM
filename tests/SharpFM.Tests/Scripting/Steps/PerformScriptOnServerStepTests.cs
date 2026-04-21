using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformScriptOnServerStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="164" name="Perform Script on Server"><WaitForCompletion state="True" /><Calculation><![CDATA[$optional_parameter]]></Calculation><Script id="5" name="Sync" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformScriptOnServerStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Script on Server", out var metadata));
        Assert.Equal(164, metadata!.Id);
    }
}
