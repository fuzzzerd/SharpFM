using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenManageThemesStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenManageThemesStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="165" name="Open Manage Themes"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenManageThemesStep.Metadata.FromXml!(source);

        Assert.IsType<OpenManageThemesStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenManageThemesStep();
        Assert.Equal("Open Manage Themes", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="165" name="Open Manage Themes"/>""");
        var step = OpenManageThemesStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Manage Themes", out var metadata));
        Assert.Equal(165, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
