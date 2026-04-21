using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ShowHideTextRulerStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="92" name="Show/Hide Text Ruler"><ShowHide value="Show"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ShowHideTextRulerStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = ShowHideTextRulerStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Show/Hide Text Ruler [ Action: Show ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = ShowHideTextRulerStep.Metadata.FromDisplay!(true, new[] { "Action: Show" });
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Show/Hide Text Ruler", out var metadata));
        Assert.Equal(92, metadata!.Id);
        Assert.Single(metadata.Params);
        Assert.Equal("enum", metadata.Params[0].Type);
    }
}
