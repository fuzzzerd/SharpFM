using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenEditSavedFindsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenEditSavedFindsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="149" name="Open Edit Saved Finds"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenEditSavedFindsStep.Metadata.FromXml!(source);

        Assert.IsType<OpenEditSavedFindsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenEditSavedFindsStep();
        Assert.Equal("Open Edit Saved Finds", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="149" name="Open Edit Saved Finds"/>""");
        var step = OpenEditSavedFindsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Edit Saved Finds", out var metadata));
        Assert.Equal(149, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
