using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for ShowAllRecordsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class ShowAllRecordsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="23" name="Show All Records"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ShowAllRecordsStep.Metadata.FromXml!(source);

        Assert.IsType<ShowAllRecordsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new ShowAllRecordsStep();
        Assert.Equal("Show All Records", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="23" name="Show All Records"/>""");
        var step = ShowAllRecordsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Show All Records", out var metadata));
        Assert.Equal(23, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
