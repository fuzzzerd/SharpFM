using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ReLoginStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="138" name="Re-Login"><NoInteract state="True"/><AccountName><Calculation><![CDATA[$x]]></Calculation></AccountName><Password><Calculation><![CDATA[$x]]></Calculation></Password></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ReLoginStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = ReLoginStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = ReLoginStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Re-Login", out var metadata));
        Assert.Equal(138, metadata!.Id);
    }
}
