using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for NewRecordRequestStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class NewRecordRequestStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="7" name="New Record/Request"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = NewRecordRequestStep.Metadata.FromXml!(source);

        Assert.IsType<NewRecordRequestStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new NewRecordRequestStep();
        Assert.Equal("New Record/Request", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="7" name="New Record/Request"/>""");
        var step = NewRecordRequestStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("New Record/Request", out var metadata));
        Assert.Equal(7, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
