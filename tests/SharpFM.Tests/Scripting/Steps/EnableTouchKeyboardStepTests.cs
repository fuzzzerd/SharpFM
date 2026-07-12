using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class EnableTouchKeyboardStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="174" name="Enable Touch Keyboard"><ShowHide value="On"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = EnableTouchKeyboardStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = EnableTouchKeyboardStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Enable Touch Keyboard [ On ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = StepDisplayFactory.TryCreate(EnableTouchKeyboardStep.XmlName, true, new[] { "On" })!;
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Enable Touch Keyboard", out var metadata));
        Assert.Equal(174, metadata!.Id);
        Assert.Single(ShapeHrView.HrNodes(metadata.Shape));
        Assert.Equal("enum", ShapeHrView.KindOf(ShapeHrView.HrNodes(metadata.Shape)[0]));
    }
}
