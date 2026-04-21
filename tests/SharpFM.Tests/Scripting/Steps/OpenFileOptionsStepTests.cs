using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenFileOptionsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenFileOptionsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="114" name="Open File Options"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenFileOptionsStep.Metadata.FromXml!(source);

        Assert.IsType<OpenFileOptionsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenFileOptionsStep();
        Assert.Equal("Open File Options", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="114" name="Open File Options"/>""");
        var step = OpenFileOptionsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open File Options", out var metadata));
        Assert.Equal(114, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
