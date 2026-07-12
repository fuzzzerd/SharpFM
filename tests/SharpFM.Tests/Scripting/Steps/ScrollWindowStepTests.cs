using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class ScrollWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="81" name="Scroll Window"><ScrollOperation value="Home"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ScrollWindowStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = ScrollWindowStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Scroll Window [ Direction: Home ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = StepDisplayFactory.TryCreate(ScrollWindowStep.XmlName, true, new[] { "Direction: Home" })!;
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Scroll Window", out var metadata));
        Assert.Equal(81, metadata!.Id);
        Assert.Single(ShapeHrView.HrNodes(metadata.Shape));
        Assert.Equal("enum", ShapeHrView.KindOf(ShapeHrView.HrNodes(metadata.Shape)[0]));
    }
}
