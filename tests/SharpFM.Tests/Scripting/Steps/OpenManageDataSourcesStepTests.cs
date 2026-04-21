using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenManageDataSourcesStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenManageDataSourcesStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="140" name="Open Manage Data Sources"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenManageDataSourcesStep.Metadata.FromXml!(source);

        Assert.IsType<OpenManageDataSourcesStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenManageDataSourcesStep();
        Assert.Equal("Open Manage Data Sources", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="140" name="Open Manage Data Sources"/>""");
        var step = OpenManageDataSourcesStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Manage Data Sources", out var metadata));
        Assert.Equal(140, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
