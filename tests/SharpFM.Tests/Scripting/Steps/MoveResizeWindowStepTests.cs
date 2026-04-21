using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class MoveResizeWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="119" name="Move/Resize Window"><Window value="ByName"/><Name><Calculation><![CDATA[$x]]></Calculation></Name><LimitToWindowsOfCurrentFile state="True"/><Height><Calculation><![CDATA[$x]]></Calculation></Height><Width><Calculation><![CDATA[$x]]></Calculation></Width><DistanceFromTop><Calculation><![CDATA[$x]]></Calculation></DistanceFromTop><DistanceFromLeft><Calculation><![CDATA[$x]]></Calculation></DistanceFromLeft></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = MoveResizeWindowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = MoveResizeWindowStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = MoveResizeWindowStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Move/Resize Window", out var metadata));
        Assert.Equal(119, metadata!.Id);
    }
}
