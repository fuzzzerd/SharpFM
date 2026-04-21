using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SaveACopyAsAddOnPackageStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="96" name="Save a Copy as Add-on Package"><LinkAvail state="True"/><Calculation><![CDATA[$x]]></Calculation></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SaveACopyAsAddOnPackageStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = SaveACopyAsAddOnPackageStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = SaveACopyAsAddOnPackageStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Save a Copy as Add-on Package", out var metadata));
        Assert.Equal(96, metadata!.Id);
    }
}
