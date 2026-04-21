using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for CopyAllRecordsRequestsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class CopyAllRecordsRequestsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="98" name="Copy All Records/Requests"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = CopyAllRecordsRequestsStep.Metadata.FromXml!(source);

        Assert.IsType<CopyAllRecordsRequestsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new CopyAllRecordsRequestsStep();
        Assert.Equal("Copy All Records/Requests", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="98" name="Copy All Records/Requests"/>""");
        var step = CopyAllRecordsRequestsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Copy All Records/Requests", out var metadata));
        Assert.Equal(98, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
