using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformJavaScriptInWebViewerStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="175" name="Perform JavaScript in Web Viewer"><ObjectName><Calculation><![CDATA[$object]]></Calculation></ObjectName><FunctionName><Calculation><![CDATA["myFunction"]]></Calculation></FunctionName><Parameters Count="1"><P><Calculation><![CDATA[$parameter1]]></Calculation></P></Parameters></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformJavaScriptInWebViewerStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Parameters_AreTyped()
    {
        var step = (PerformJavaScriptInWebViewerStep)PerformJavaScriptInWebViewerStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Single(step.Parameters);
        Assert.Equal("$parameter1", step.Parameters[0].Text);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform JavaScript in Web Viewer", out var metadata));
        Assert.Equal(175, metadata!.Id);
    }
}
