using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class AVPlayerSetPlaybackStateStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="178" name="AVPlayer Set Playback State"><PlaybackState value="Stopped"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = AVPlayerSetPlaybackStateStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = AVPlayerSetPlaybackStateStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("AVPlayer Set Playback State [ Stopped ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = StepDisplayFactory.TryCreate(AVPlayerSetPlaybackStateStep.XmlName, true, new[] { "Stopped" })!;
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("AVPlayer Set Playback State", out var metadata));
        Assert.Equal(178, metadata!.Id);
        Assert.Single(ShapeHrView.HrNodes(metadata.Shape));
        Assert.Equal("enum", ShapeHrView.KindOf(ShapeHrView.HrNodes(metadata.Shape)[0]));
    }
}
