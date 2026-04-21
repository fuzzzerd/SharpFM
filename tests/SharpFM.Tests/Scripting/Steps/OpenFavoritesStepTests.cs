using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenFavoritesStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenFavoritesStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="183" name="Open Favorites"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenFavoritesStep.Metadata.FromXml!(source);

        Assert.IsType<OpenFavoritesStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenFavoritesStep();
        Assert.Equal("Open Favorites", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="183" name="Open Favorites"/>""");
        var step = OpenFavoritesStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Favorites", out var metadata));
        Assert.Equal(183, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
