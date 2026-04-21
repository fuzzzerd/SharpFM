using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for EditUserDictionaryStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class EditUserDictionaryStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="109" name="Edit User Dictionary"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = EditUserDictionaryStep.Metadata.FromXml!(source);

        Assert.IsType<EditUserDictionaryStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new EditUserDictionaryStep();
        Assert.Equal("Edit User Dictionary", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="109" name="Edit User Dictionary"/>""");
        var step = EditUserDictionaryStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Edit User Dictionary", out var metadata));
        Assert.Equal(109, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
