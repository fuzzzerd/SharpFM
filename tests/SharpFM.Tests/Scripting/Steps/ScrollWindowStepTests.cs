using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ScrollWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="81" name="Scroll Window"><ScrollOperation value="Home"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ScrollWindowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = ScrollWindowStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Scroll Window [ Direction: Home ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = ScrollWindowStep.Metadata.FromDisplay!(true, new[] { "Direction: Home" });
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Scroll Window", out var metadata));
        Assert.Equal(81, metadata!.Id);
        Assert.Single(metadata.Params);
        Assert.Equal("enum", metadata.Params[0].Type);
    }
}
