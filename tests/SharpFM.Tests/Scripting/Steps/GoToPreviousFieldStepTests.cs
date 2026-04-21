using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for GoToPreviousFieldStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class GoToPreviousFieldStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="4" name="Go to Previous Field"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GoToPreviousFieldStep.Metadata.FromXml!(source);

        Assert.IsType<GoToPreviousFieldStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new GoToPreviousFieldStep();
        Assert.Equal("Go to Previous Field", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="4" name="Go to Previous Field"/>""");
        var step = GoToPreviousFieldStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Go to Previous Field", out var metadata));
        Assert.Equal(4, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
