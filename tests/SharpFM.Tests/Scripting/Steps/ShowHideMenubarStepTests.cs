using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ShowHideMenubarStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="166" name="Show/Hide Menubar"><Lock state="True"/><ShowHide value="Show"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ShowHideMenubarStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = ShowHideMenubarStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();

        // Extract the tokens inside [ ... ] and feed through FromDisplay.
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = ShowHideMenubarStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Show/Hide Menubar", out var metadata));
        Assert.Equal(166, metadata!.Id);
        Assert.Equal(2, metadata.Params.Count);
    }
}
