using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for FlushCacheToDiskStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class FlushCacheToDiskStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="102" name="Flush Cache to Disk"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = FlushCacheToDiskStep.Metadata.FromXml!(source);

        Assert.IsType<FlushCacheToDiskStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new FlushCacheToDiskStep();
        Assert.Equal("Flush Cache to Disk", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="102" name="Flush Cache to Disk"/>""");
        var step = FlushCacheToDiskStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Flush Cache to Disk", out var metadata));
        Assert.Equal(102, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
