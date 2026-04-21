using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Zero-parameter POCO canonical pattern. Fixtures are inline raw string
/// literals copied from agentic-fm's snippet_examples so the test reads
/// without having to open a separate file.
/// </summary>
public class BeepStepTests
{
    // Canonical shape from
    //   agent/snippet_examples/steps/miscellaneous/Beep.xml
    private const string CanonicalSnippet = """
        <?xml version="1.0"?>
        <fmxmlsnippet type="FMObjectList">
          <Step enable="True" id="93" name="Beep"/>
        </fmxmlsnippet>
        """;

    private static XElement CanonicalStepElement() =>
        XDocument.Parse(CanonicalSnippet).Root!.Element("Step")!;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = CanonicalStepElement();
        var step = BeepStep.Metadata.FromXml!(source);

        Assert.IsType<BeepStep>(step);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsBareName()
    {
        var step = new BeepStep();
        Assert.Equal("Beep", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_RoundTrips()
    {
        var source = XElement.Parse("""<Step enable="False" id="93" name="Beep"/>""");
        var step = BeepStep.Metadata.FromXml!(source);

        Assert.False(step.Enabled);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasBeep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Beep", out var metadata));
        Assert.Equal(93, metadata!.Id);
        Assert.Equal("miscellaneous", metadata.Category);
        Assert.Null(metadata.BlockPair);
        Assert.Empty(metadata.Params);
    }
}
