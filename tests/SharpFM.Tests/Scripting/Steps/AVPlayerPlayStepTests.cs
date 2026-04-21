using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AVPlayerPlayStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="177" name="AVPlayer Play"><Source value="Object Name"/><Repetition><Calculation><![CDATA[$x]]></Calculation></Repetition><Presentation value="Start Full Screen"/><PlaybackPosition><Calculation><![CDATA[$x]]></Calculation></PlaybackPosition><StartOffset><Calculation><![CDATA[$x]]></Calculation></StartOffset><EndOffset><Calculation><![CDATA[$x]]></Calculation></EndOffset><HideControls value="True"/><DisableInteraction value="True"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = AVPlayerPlayStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("AVPlayer Play", out var metadata));
        Assert.Equal(177, metadata!.Id);
    }
}
