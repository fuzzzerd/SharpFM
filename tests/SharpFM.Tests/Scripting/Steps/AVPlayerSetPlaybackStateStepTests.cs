using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AVPlayerSetPlaybackStateStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="178" name="AVPlayer Set Playback State"><PlaybackState value="Stopped"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = AVPlayerSetPlaybackStateStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = AVPlayerSetPlaybackStateStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("AVPlayer Set Playback State [ Stopped ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = AVPlayerSetPlaybackStateStep.Metadata.FromDisplay!(true, new[] { "Stopped" });
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("AVPlayer Set Playback State", out var metadata));
        Assert.Equal(178, metadata!.Id);
        Assert.Single(metadata.Params);
        Assert.Equal("enum", metadata.Params[0].Type);
    }
}
