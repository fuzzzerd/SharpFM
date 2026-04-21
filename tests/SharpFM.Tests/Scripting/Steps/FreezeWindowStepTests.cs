using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for FreezeWindowStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class FreezeWindowStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="79" name="Freeze Window"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = FreezeWindowStep.Metadata.FromXml!(source);

        Assert.IsType<FreezeWindowStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new FreezeWindowStep();
        Assert.Equal("Freeze Window", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="79" name="Freeze Window"/>""");
        var step = FreezeWindowStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Freeze Window", out var metadata));
        Assert.Equal(79, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
