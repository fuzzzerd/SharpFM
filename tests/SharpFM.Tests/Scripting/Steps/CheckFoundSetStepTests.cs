using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for CheckFoundSetStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class CheckFoundSetStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="20" name="Check Found Set"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = CheckFoundSetStep.Metadata.FromXml!(source);

        Assert.IsType<CheckFoundSetStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new CheckFoundSetStep();
        Assert.Equal("Check Found Set", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="20" name="Check Found Set"/>""");
        var step = CheckFoundSetStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Check Found Set", out var metadata));
        Assert.Equal(20, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
