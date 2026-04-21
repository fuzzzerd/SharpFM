using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InstallOnTimerScriptStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="148" name="Install OnTimer Script"><Script id="5" name="Refresh" /><Interval><Calculation><![CDATA[30]]></Calculation></Interval></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InstallOnTimerScriptStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsScriptAndInterval()
    {
        var step = (InstallOnTimerScriptStep)InstallOnTimerScriptStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Install OnTimer Script [ \"Refresh\" ; Interval: 30 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Install OnTimer Script", out var metadata));
        Assert.Equal(148, metadata!.Id);
    }
}
