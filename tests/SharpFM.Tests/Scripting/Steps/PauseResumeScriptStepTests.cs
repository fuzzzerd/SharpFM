using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PauseResumeScriptStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="62" name="Pause/Resume Script"><PauseTime value="Indefinitely"/><Calculation><![CDATA[$x]]></Calculation></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PauseResumeScriptStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Pause/Resume Script", out var metadata));
        Assert.Equal(62, metadata!.Id);
    }
}
