using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for ShowOmittedOnlyStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class ShowOmittedOnlyStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="27" name="Show Omitted Only"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ShowOmittedOnlyStep.Metadata.FromXml!(source);

        Assert.IsType<ShowOmittedOnlyStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new ShowOmittedOnlyStep();
        Assert.Equal("Show Omitted Only", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="27" name="Show Omitted Only"/>""");
        var step = ShowOmittedOnlyStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Show Omitted Only", out var metadata));
        Assert.Equal(27, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
