using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InstallMenuSetStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="142" name="Install Menu Set"><UseAsFileDefault state="False" /><CustomMenuSet id="1" name="[Standard FileMaker Menus]" /></Step>
        """;

    private const string QuoteInNameXml = """
        <Step enable="True" id="142" name="Install Menu Set"><UseAsFileDefault state="False" /><CustomMenuSet id="3" name="O&quot;Brien Menu" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InstallMenuSetStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsMenuSetAndFlag()
    {
        var step = InstallMenuSetStep.Parse(XElement.Parse(CanonicalXml));
        Assert.Equal("Install Menu Set [ \"[Standard FileMaker Menus]\" ; Use as file default: Off ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Install Menu Set", out var metadata));
        Assert.Equal(142, metadata!.Id);
    }

    [Fact]
    public void Display_QuoteInName_DoublesEmbeddedQuote()
    {
        var step = InstallMenuSetStep.Parse(XElement.Parse(QuoteInNameXml));
        Assert.Equal("Install Menu Set [ \"O\"\"Brien Menu\" ; Use as file default: Off ]", step.ToDisplayLine());
    }

    [Fact]
    public void FullRoundTrip_QuoteInName_PreservesName()
    {
        var step1 = InstallMenuSetStep.Parse(XElement.Parse(QuoteInNameXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();
        Assert.Equal("O\"Brien Menu", xml.Element("CustomMenuSet")!.Attribute("name")!.Value);
    }
}
