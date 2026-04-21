using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformAppleScriptStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="67" name="Perform AppleScript"><ContentType value="Calculation"/><Calculation><![CDATA[$x]]></Calculation><Text>$example</Text></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformAppleScriptStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform AppleScript", out var metadata));
        Assert.Equal(67, metadata!.Id);
    }
}
