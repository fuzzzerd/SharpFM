using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

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
        var step = InsertAudioVideoStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsPathAndReference()
    {
        var step = InsertAudioVideoStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Insert Audio/Video [ $path ; Reference: Embedded ]", step.ToDisplayLine());
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = InsertAudioVideoStep.Parse(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = StepDisplayFactory.TryCreate(InsertAudioVideoStep.XmlName, true, tokens)!;
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Audio/Video", out var metadata));
        Assert.Equal(159, metadata!.Id);
    }
}
