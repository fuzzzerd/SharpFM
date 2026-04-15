using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetVariableStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string SimpleXml =
        "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
        + "<Value><Calculation><![CDATA[0]]></Calculation></Value>"
        + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
        + "<Name>$count</Name>"
        + "</Step>";

    private const string LiteralRepetitionXml =
        "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
        + "<Value><Calculation><![CDATA[\"third\"]]></Calculation></Value>"
        + "<Repetition><Calculation><![CDATA[3]]></Calculation></Repetition>"
        + "<Name>$arr</Name>"
        + "</Step>";

    private const string CalculatedRepetitionXml =
        "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
        + "<Value><Calculation><![CDATA[$count + 5]]></Calculation></Value>"
        + "<Repetition><Calculation><![CDATA[$anotherVariable]]></Calculation></Repetition>"
        + "<Name>$arr</Name>"
        + "</Step>";

    [Fact]
    public void Simple_Display_SuppressesDefaultRepetition()
    {
        var step = ScriptStep.FromXml(MakeStep(SimpleXml));
        Assert.Equal("Set Variable [ $count ; Value: 0 ]", step.ToDisplayLine());
    }

    [Fact]
    public void LiteralRepetition_Display_ShowsBracketedRep()
    {
        var step = ScriptStep.FromXml(MakeStep(LiteralRepetitionXml));
        Assert.Equal("Set Variable [ $arr[3] ; Value: \"third\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void CalculatedRepetition_Display_PreservesCalcExpression()
    {
        var step = ScriptStep.FromXml(MakeStep(CalculatedRepetitionXml));
        Assert.Equal(
            "Set Variable [ $arr[$anotherVariable] ; Value: $count + 5 ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void Simple_RoundTrip_AlwaysEmitsRepetitionElement()
    {
        // Repetition is always emitted even at value "1" — that's the
        // round-trip invariant from the inventory.
        var step = ScriptStep.FromXml(MakeStep(SimpleXml));
        var xml = step.ToXml();

        Assert.NotNull(xml.Element("Repetition"));
        Assert.Equal("1", xml.Element("Repetition")!.Element("Calculation")!.Value);
    }

    [Fact]
    public void CalculatedRepetition_RoundTrip_PreservesVariableExpression()
    {
        var step = ScriptStep.FromXml(MakeStep(CalculatedRepetitionXml));
        var xml = step.ToXml();

        Assert.Equal("$anotherVariable", xml.Element("Repetition")!.Element("Calculation")!.Value);
        Assert.Equal("$count + 5", xml.Element("Value")!.Element("Calculation")!.Value);
        Assert.Equal("$arr", xml.Element("Name")!.Value);
    }

    [Fact]
    public void XmlParamOrder_ValueRepetitionName()
    {
        var step = ScriptStep.FromXml(MakeStep(SimpleXml));
        var xml = step.ToXml();
        var children = xml.Elements().Select(e => e.Name.LocalName).ToArray();
        Assert.Equal(new[] { "Value", "Repetition", "Name" }, children);
    }

    [Fact]
    public void FullRoundTrip_LiteralRep_PreservesRepetitionValue()
    {
        var step1 = ScriptStep.FromXml(MakeStep(LiteralRepetitionXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        Assert.Equal("3", xml.Element("Repetition")!.Element("Calculation")!.Value);
        Assert.Equal("$arr", xml.Element("Name")!.Value);
    }
}
