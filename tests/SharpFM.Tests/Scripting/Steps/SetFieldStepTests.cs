using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Verbatim FM Pro clipboard fixtures for the typed SetFieldStep. Fixtures
/// captured via the Raw Clipboard Viewer diagnostic against a script in
/// <c>FileMakerDbs/</c> (see docs/step-definitions.md).
/// </summary>
public class SetFieldStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string LiteralStringXml =
        "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
        + "<Calculation><![CDATA[\"just-a-string\"]]></Calculation>"
        + "<Field table=\"ScriptDefinitionHelper\" id=\"5\" name=\"ModifiedBy\"></Field>"
        + "</Step>";

    private const string VariableCalcXml =
        "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
        + "<Calculation><![CDATA[$variable + \" string\"]]></Calculation>"
        + "<Field table=\"ScriptDefinitionHelper\" id=\"5\" name=\"ModifiedBy\"></Field>"
        + "</Step>";

    private const string MultiLineCalcXml =
        "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
        + "<Calculation><![CDATA[ScriptDefinitionHelper::PrimaryKey \n& \" \" \n& ScriptDefinitionHelper::CreatedBy]]></Calculation>"
        + "<Field table=\"ScriptDefinitionHelper\" id=\"5\" name=\"ModifiedBy\"></Field>"
        + "</Step>";

    [Fact]
    public void LiteralString_Display_UsesTargetFirstCalcSecond()
    {
        var step = ScriptStep.FromXml(MakeStep(LiteralStringXml));
        Assert.Equal(
            "Set Field [ ScriptDefinitionHelper::ModifiedBy (#5) ; \"just-a-string\" ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void LiteralString_RoundTrip_PreservesFieldAndCalc()
    {
        var step = ScriptStep.FromXml(MakeStep(LiteralStringXml));
        var xml = step.ToXml();

        var field = xml.Element("Field");
        Assert.NotNull(field);
        Assert.Equal("ScriptDefinitionHelper", field!.Attribute("table")!.Value);
        Assert.Equal("5", field.Attribute("id")!.Value);
        Assert.Equal("ModifiedBy", field.Attribute("name")!.Value);

        Assert.Equal("\"just-a-string\"", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void VariableCalc_Display_ShowsVariableInCalc()
    {
        var step = ScriptStep.FromXml(MakeStep(VariableCalcXml));
        Assert.Equal(
            "Set Field [ ScriptDefinitionHelper::ModifiedBy (#5) ; $variable + \" string\" ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void MultiLineCalc_RoundTrip_PreservesNewlinesInCdata()
    {
        var step = ScriptStep.FromXml(MakeStep(MultiLineCalcXml));
        var xml = step.ToXml();
        Assert.Contains("\n", xml.Element("Calculation")!.Value);
        Assert.Contains("ScriptDefinitionHelper::PrimaryKey", xml.Element("Calculation")!.Value);
        Assert.Contains("ScriptDefinitionHelper::CreatedBy", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void XmlParamOrder_CalculationBeforeField()
    {
        var step = ScriptStep.FromXml(MakeStep(LiteralStringXml));
        var xml = step.ToXml();
        var firstChild = xml.Elements().First();
        Assert.Equal("Calculation", firstChild.Name.LocalName);
        Assert.Equal("Field", xml.Elements().ElementAt(1).Name.LocalName);
    }

    [Fact]
    public void FullRoundTrip_FromDisplay_PreservesFieldTable()
    {
        var step1 = ScriptStep.FromXml(MakeStep(LiteralStringXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        var field = xml.Element("Field");
        Assert.Equal("ScriptDefinitionHelper", field!.Attribute("table")?.Value);
        Assert.Equal("ModifiedBy", field.Attribute("name")?.Value);
    }
}
