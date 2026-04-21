using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for CopyRecordRequestStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class CopyRecordRequestStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="101" name="Copy Record/Request"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = CopyRecordRequestStep.Metadata.FromXml!(source);

        Assert.IsType<CopyRecordRequestStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new CopyRecordRequestStep();
        Assert.Equal("Copy Record/Request", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="101" name="Copy Record/Request"/>""");
        var step = CopyRecordRequestStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Copy Record/Request", out var metadata));
        Assert.Equal(101, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
