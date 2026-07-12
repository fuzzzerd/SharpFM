using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class SaveRecordsAsSnapshotLinkStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="152" name="Save Records as Snapshot Link"><CreateDirectories state="True"/><CreateEmail state="True"/><SaveType value="BrowsedRecords"/><UniversalPathList>$example</UniversalPathList></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SaveRecordsAsSnapshotLinkStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = SaveRecordsAsSnapshotLinkStep.Parse(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = StepDisplayFactory.TryCreate(SaveRecordsAsSnapshotLinkStep.XmlName, true, tokens)!;
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Save Records as Snapshot Link", out var metadata));
        Assert.Equal(152, metadata!.Id);
    }
}
