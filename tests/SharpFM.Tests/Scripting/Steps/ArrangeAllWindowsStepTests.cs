using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class ArrangeAllWindowsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="120" name="Arrange All Windows"><WindowArrangement value="Tile Horizontally"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ArrangeAllWindowsStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = ArrangeAllWindowsStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Arrange All Windows [ Tile Horizontally ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = StepDisplayFactory.TryCreate(ArrangeAllWindowsStep.XmlName, true, new[] { "Tile Horizontally" })!;
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Arrange All Windows", out var metadata));
        Assert.Equal(120, metadata!.Id);
        Assert.Single(ShapeHrView.HrNodes(metadata.Shape));
        Assert.Equal("enum", ShapeHrView.KindOf(ShapeHrView.HrNodes(metadata.Shape)[0]));
    }
}
