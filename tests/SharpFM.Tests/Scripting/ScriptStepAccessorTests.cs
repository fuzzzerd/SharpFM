using System.Xml.Linq;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class ScriptStepAccessorTests
{
    private static ScriptStep FromXml(string xml) =>
        ScriptStep.FromXml(XElement.Parse(xml));

    [Fact]
    public void StepName_ReturnsDefinitionName()
    {
        var step = FromXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");
        Assert.Equal("Beep", step.StepName);
    }

    [Fact]
    public void StepName_UnknownStep_ReturnsFallback()
    {
        var step = FromXml("<Step enable=\"True\" id=\"9999\" name=\"FakeStep\"/>");
        Assert.Equal("FakeStep", step.StepName);
    }

    [Fact]
    public void GetCalculation_ReturnsCalcValue()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");

        Assert.Equal("$x > 0", step.GetCalculation());
    }

    [Fact]
    public void GetCalculation_NoCalc_ReturnsNull()
    {
        var step = FromXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");
        Assert.Null(step.GetCalculation());
    }

    [Fact]
    public void GetFieldReference_ReturnsTableField()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Field table=\"People\" id=\"1\" name=\"FirstName\"/>"
            + "<Calculation><![CDATA[\"test\"]]></Calculation></Step>");

        Assert.Equal("People::FirstName", step.GetFieldReference());
    }

    [Fact]
    public void GetScriptReference_ReturnsUnquotedName()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Script id=\"1\" name=\"Initialize\"/>"
            + "<Calculation><![CDATA[]]></Calculation></Step>");

        Assert.Equal("Initialize", step.GetScriptReference());
    }

    [Fact]
    public void GetLayoutReference_ReturnsUnquotedName()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"1\" name=\"Detail\"/></Step>");

        Assert.Equal("Detail", step.GetLayoutReference());
    }

    [Fact]
    public void GetNamedParam_ByLabel()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>");

        Assert.Equal("$count + 1", step.GetNamedParam("Value"));
    }

    [Fact]
    public void GetParamByXmlElement_ReturnsValue()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>");

        Assert.Equal("$count", step.GetParamByXmlElement("Name"));
    }

    [Fact]
    public void GetNamedParam_CaseInsensitive()
    {
        var step = FromXml(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[42]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$x</Name></Step>");

        Assert.Equal("42", step.GetNamedParam("value"));
    }
}
