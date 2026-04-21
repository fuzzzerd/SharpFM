using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class NewWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="122" name="New Window"><LayoutDestination value="SelectedLayout"/><Name><Calculation><![CDATA[$x]]></Calculation></Name><Height><Calculation><![CDATA[$x]]></Calculation></Height><Width><Calculation><![CDATA[$x]]></Calculation></Width><DistanceFromTop><Calculation><![CDATA[$x]]></Calculation></DistanceFromTop><DistanceFromLeft><Calculation><![CDATA[$x]]></Calculation></DistanceFromLeft><NewWndStyles>$example</NewWndStyles></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = NewWindowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("New Window", out var metadata));
        Assert.Equal(122, metadata!.Id);
    }
}
