using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenManageLayoutsStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenManageLayoutsStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="151" name="Open Manage Layouts"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenManageLayoutsStep.Metadata.FromXml!(source);

        Assert.IsType<OpenManageLayoutsStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenManageLayoutsStep();
        Assert.Equal("Open Manage Layouts", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="151" name="Open Manage Layouts"/>""");
        var step = OpenManageLayoutsStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Manage Layouts", out var metadata));
        Assert.Equal(151, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
