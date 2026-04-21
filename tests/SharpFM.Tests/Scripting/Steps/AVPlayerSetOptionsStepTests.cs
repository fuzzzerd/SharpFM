using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AVPlayerSetOptionsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="179" name="AVPlayer Set Options"><Presentation value="Start Full Screen"/><DisableInteraction value="True"/><HideControls value="True"/><DisableExternalControls value="True"/><PauseInBackground value="True"/><PlaybackPosition><Calculation><![CDATA[$x]]></Calculation></PlaybackPosition><StartOffset><Calculation><![CDATA[$x]]></Calculation></StartOffset><EndOffset><Calculation><![CDATA[$x]]></Calculation></EndOffset><Volume><Calculation><![CDATA[$x]]></Calculation></Volume><Zoom value="Fit"/><Sequence value="None"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = AVPlayerSetOptionsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("AVPlayer Set Options", out var metadata));
        Assert.Equal(179, metadata!.Id);
    }
}
