using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class CloseWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="121" name="Close Window"><LimitToWindowsOfCurrentFile state="True"/><Window value="ByName"/><Name><Calculation><![CDATA[$x]]></Calculation></Name></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = CloseWindowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Close Window", out var metadata));
        Assert.Equal(121, metadata!.Id);
    }
}
