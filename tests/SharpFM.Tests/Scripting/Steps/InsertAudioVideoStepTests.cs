using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertAudioVideoStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="159" name="Insert Audio/Video"><UniversalPathList type="Embedded">$path</UniversalPathList></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertAudioVideoStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsPathAndReference()
    {
        var step = (InsertAudioVideoStep)InsertAudioVideoStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Insert Audio/Video [ $path ; Reference: Embedded ]", step.ToDisplayLine());
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = InsertAudioVideoStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = InsertAudioVideoStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Audio/Video", out var metadata));
        Assert.Equal(159, metadata!.Id);
    }
}
