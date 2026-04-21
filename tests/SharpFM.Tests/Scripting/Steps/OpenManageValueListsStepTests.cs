using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenManageValueListsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenManageValueListsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="112" name="Open Manage Value Lists"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenManageValueListsStep.Metadata.FromXml!(source);

        Assert.IsType<OpenManageValueListsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenManageValueListsStep();
        Assert.Equal("Open Manage Value Lists", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="112" name="Open Manage Value Lists"/>""");
        var step = OpenManageValueListsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Manage Value Lists", out var metadata));
        Assert.Equal(112, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
