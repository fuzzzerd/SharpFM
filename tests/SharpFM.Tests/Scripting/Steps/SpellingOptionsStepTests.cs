using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for SpellingOptionsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class SpellingOptionsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="107" name="Spelling Options"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SpellingOptionsStep.Metadata.FromXml!(source);

        Assert.IsType<SpellingOptionsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new SpellingOptionsStep();
        Assert.Equal("Spelling Options", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="107" name="Spelling Options"/>""");
        var step = SpellingOptionsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Spelling Options", out var metadata));
        Assert.Equal(107, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
