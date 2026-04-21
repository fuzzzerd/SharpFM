using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenFindReplaceStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenFindReplaceStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="129" name="Open Find/Replace"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenFindReplaceStep.Metadata.FromXml!(source);

        Assert.IsType<OpenFindReplaceStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenFindReplaceStep();
        Assert.Equal("Open Find/Replace", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="129" name="Open Find/Replace"/>""");
        var step = OpenFindReplaceStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Find/Replace", out var metadata));
        Assert.Equal(129, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
