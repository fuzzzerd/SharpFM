using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class DialPhoneStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="65" name="Dial Phone"><NoInteract state="True"/><UseDialPreferences value="True"/><Calculation><![CDATA[$x]]></Calculation></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = DialPhoneStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Dial Phone", out var metadata));
        Assert.Equal(65, metadata!.Id);
    }
}
