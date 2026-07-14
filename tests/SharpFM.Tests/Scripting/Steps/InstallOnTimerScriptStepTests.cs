using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InstallOnTimerScriptStepTests
{
    // Canonical §8.1 element order: Interval -> Script (was Script -> Interval).
    private const string CanonicalXml = """
        <Step enable="True" id="148" name="Install OnTimer Script"><Interval><Calculation><![CDATA[30]]></Calculation></Interval><Script id="5" name="Refresh" /></Step>
        """;

    private const string QuoteInNameXml = """
        <Step enable="True" id="148" name="Install OnTimer Script"><Interval><Calculation><![CDATA[30]]></Calculation></Interval><Script id="5" name="O&quot;Brien" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InstallOnTimerScriptStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsScriptAndInterval()
    {
        var step = InstallOnTimerScriptStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Install OnTimer Script [ \"Refresh\" ; Interval: 30 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Install OnTimer Script", out var metadata));
        Assert.Equal(148, metadata!.Id);
    }

    [Fact]
    public void Display_QuoteInName_DoublesEmbeddedQuote()
    {
        var step = InstallOnTimerScriptStep.Parse(XElement.Parse(QuoteInNameXml));
        Assert.Equal("Install OnTimer Script [ \"O\"\"Brien\" ; Interval: 30 ]", step.ToDisplayLine());
    }

    [Fact]
    public void FullRoundTrip_QuoteInName_PreservesName()
    {
        var step1 = InstallOnTimerScriptStep.Parse(XElement.Parse(QuoteInNameXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();
        Assert.Equal("O\"Brien", xml.Element("Script")!.Attribute("name")!.Value);
    }
}
