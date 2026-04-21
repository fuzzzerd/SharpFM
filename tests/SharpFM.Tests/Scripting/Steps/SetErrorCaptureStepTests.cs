using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Single-boolean POCO canonical pattern. The underlying XML carries a
/// <c>&lt;Set state="True|False"/&gt;</c> child; display text uses
/// "On"/"Off" matching FileMaker Pro's wording.
/// </summary>
public class SetErrorCaptureStepTests
{
    // Canonical shape from
    //   agent/snippet_examples/steps/control/Set Error Capture.xml
    private const string StateTrueSnippet = """
        <?xml version="1.0"?>
        <fmxmlsnippet type="FMObjectList">
          <Step enable="True" id="86" name="Set Error Capture">
            <Set state="True"/>
          </Step>
        </fmxmlsnippet>
        """;

    private const string StateFalseSnippet = """
        <?xml version="1.0"?>
        <fmxmlsnippet type="FMObjectList">
          <Step enable="True" id="86" name="Set Error Capture">
            <Set state="False"/>
          </Step>
        </fmxmlsnippet>
        """;

    private static XElement StepFrom(string snippet) =>
        XDocument.Parse(snippet).Root!.Element("Step")!;

    [Fact]
    public void RoundTrip_StateTrue()
    {
        var source = StepFrom(StateTrueSnippet);
        var step = (SetErrorCaptureStep)SetErrorCaptureStep.Metadata.FromXml!(source);

        Assert.True(step.CaptureErrors);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_StateFalse()
    {
        var source = StepFrom(StateFalseSnippet);
        var step = (SetErrorCaptureStep)SetErrorCaptureStep.Metadata.FromXml!(source);

        Assert.False(step.CaptureErrors);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsOnOff()
    {
        Assert.Equal("Set Error Capture [ On ]",
            new SetErrorCaptureStep(captureErrors: true).ToDisplayLine());
        Assert.Equal("Set Error Capture [ Off ]",
            new SetErrorCaptureStep(captureErrors: false).ToDisplayLine());
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step = (SetErrorCaptureStep)SetErrorCaptureStep.Metadata.FromDisplay!(true, new[] { "On" });
        Assert.True(step.CaptureErrors);

        var off = (SetErrorCaptureStep)SetErrorCaptureStep.Metadata.FromDisplay!(true, new[] { "Off" });
        Assert.False(off.CaptureErrors);
    }

    [Fact]
    public void Registry_HasSetErrorCaptureWithParam()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Error Capture", out var metadata));
        Assert.Equal(86, metadata!.Id);
        Assert.Equal("control", metadata.Category);
        Assert.Null(metadata.BlockPair);

        var param = Assert.Single(metadata.Params);
        Assert.Equal("Set", param.XmlElement);
        Assert.Equal("boolean", param.Type);
        Assert.Equal("state", param.XmlAttr);

        // Description is sourced from agentic-fm's snippet comment
        // and powers tooltip / hover UIs as they're wired up later.
        Assert.NotNull(param.Description);
        Assert.Contains("suppresses", param.Description, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidValues_ReturnsOnAndOff()
    {
        var metadata = StepRegistry.ByName["Set Error Capture"];
        var values = StepRegistry.GetValidValues(metadata.Params[0]);
        Assert.Contains("On", values);
        Assert.Contains("Off", values);
    }
}
