using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for SelectDictionariesStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class SelectDictionariesStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="108" name="Select Dictionaries"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SelectDictionariesStep.Metadata.FromXml!(source);

        Assert.IsType<SelectDictionariesStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new SelectDictionariesStep();
        Assert.Equal("Select Dictionaries", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="108" name="Select Dictionaries"/>""");
        var step = SelectDictionariesStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Select Dictionaries", out var metadata));
        Assert.Equal(108, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
