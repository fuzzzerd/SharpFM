using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for ModifyLastFindStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class ModifyLastFindStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="24" name="Modify Last Find"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ModifyLastFindStep.Metadata.FromXml!(source);

        Assert.IsType<ModifyLastFindStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new ModifyLastFindStep();
        Assert.Equal("Modify Last Find", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="24" name="Modify Last Find"/>""");
        var step = ModifyLastFindStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Modify Last Find", out var metadata));
        Assert.Equal(24, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
