using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-param POCO tests for OpenScriptWorkspaceStep. Fixture is inline per the
/// pilot pattern; no FixtureLoader, no file I/O.
/// </summary>
public class OpenScriptWorkspaceStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="88" name="Open Script Workspace"/>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenScriptWorkspaceStep.Metadata.FromXml!(source);

        Assert.IsType<OpenScriptWorkspaceStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new OpenScriptWorkspaceStep();
        Assert.Equal("Open Script Workspace", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="88" name="Open Script Workspace"/>""");
        var step = OpenScriptWorkspaceStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Script Workspace", out var metadata));
        Assert.Equal(88, metadata!.Id);
        Assert.Empty(metadata.Params);
    }
}
