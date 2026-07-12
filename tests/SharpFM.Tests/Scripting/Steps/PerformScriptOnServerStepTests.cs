using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformScriptOnServerStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="164" name="Perform Script on Server"><WaitForCompletion state="True" /><Calculation><![CDATA[$optional_parameter]]></Calculation><Script id="5" name="Sync" /></Step>
        """;

    private const string QuoteInScriptNameXml = """
        <Step enable="True" id="164" name="Perform Script on Server"><WaitForCompletion state="True" /><Script id="5" name="O&quot;Brien Sync" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformScriptOnServerStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Script on Server", out var metadata));
        Assert.Equal(164, metadata!.Id);
    }

    [Fact]
    public void Display_QuoteInScriptName_DoublesEmbeddedQuote()
    {
        var step = PerformScriptOnServerStep.Parse(XElement.Parse(QuoteInScriptNameXml));
        Assert.Equal("Perform Script on Server [ Wait for completion: On ; \"O\"\"Brien Sync\" (#5) ]", step.ToDisplayLine());
    }

    [Fact]
    public void FullRoundTrip_QuoteInScriptName_PreservesNameAndId()
    {
        var step1 = PerformScriptOnServerStep.Parse(XElement.Parse(QuoteInScriptNameXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        var script = xml.Element("Script");
        Assert.NotNull(script);
        Assert.Equal("5", script!.Attribute("id")!.Value);
        Assert.Equal("O\"Brien Sync", script.Attribute("name")!.Value);
    }
}
