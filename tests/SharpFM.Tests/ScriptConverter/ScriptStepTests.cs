using System.Linq;
using System.Xml.Linq;
using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class ScriptStepTests
{
    private static XElement MakeStep(string xml) =>
        XElement.Parse(xml);

    [Fact]
    public void Comment_FromXml_ToDisplayLine()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello world</Text></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("# (comment)", step.Definition?.Name);
        Assert.Equal("# hello world", step.ToDisplayLine());
    }

    [Fact]
    public void Comment_FromDisplayLine_ToXml()
    {
        var step = ScriptStep.FromDisplayLine("# hello world");
        Assert.Equal("# (comment)", step.Definition?.Name);
        var xml = step.ToXml();
        Assert.Equal("hello world", xml.Element("Text")?.Value);
    }

    [Fact]
    public void SetVariable_FromXml_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("Set Variable [ $count ; Value: $count + 1 ]", step.ToDisplayLine());
    }

    [Fact]
    public void SetVariable_WithRepetition_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[\"x\"]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[2]]></Calculation></Repetition>"
            + "<Name>$arr</Name></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("Set Variable [ $arr[2] ; Value: \"x\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void SetField_FromXml_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[\"Done\"]]></Calculation>"
            + "<Field table=\"Invoices\" id=\"3\" name=\"Status\"/></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("Set Field [ Invoices::Status ; \"Done\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void PerformScript_FromXml_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Calculation><![CDATA[$param]]></Calculation>"
            + "<Script id=\"5\" name=\"Process Records\"/></Step>");
        var step = ScriptStep.FromXml(el);
        var display = step.ToDisplayLine();
        Assert.Contains("\"Process Records\"", display);
        Assert.Contains("Parameter: $param", display);
    }

    [Fact]
    public void GoToLayout_Named_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"3\" name=\"Invoices\"/></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Contains("\"Invoices\"", step.ToDisplayLine());
    }

    [Fact]
    public void GoToLayout_Original_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"OriginalLayout\"/></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Contains("original layout", step.ToDisplayLine());
    }

    [Fact]
    public void GoToRecord_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + "<RowPageLocation value=\"Next\"/>"
            + "<Exit state=\"True\"/></Step>");
        var step = ScriptStep.FromXml(el);
        var display = step.ToDisplayLine();
        Assert.Contains("Next", display);
        Assert.Contains("Exit after last: On", display);
    }

    [Fact]
    public void ShowCustomDialog_ToDisplayLine()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
            + "<Title><Calculation><![CDATA[\"Warning\"]]></Calculation></Title>"
            + "<Message><Calculation><![CDATA[\"Are you sure?\"]]></Calculation></Message>"
            + "<Buttons>"
            + "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
            + "<Button CommitState=\"True\"><Calculation><![CDATA[\"Cancel\"]]></Calculation></Button>"
            + "</Buttons></Step>");
        var step = ScriptStep.FromXml(el);
        var display = step.ToDisplayLine();
        Assert.Contains("Title: \"Warning\"", display);
        Assert.Contains("Buttons: \"OK\", \"Cancel\"", display);
    }

    [Fact]
    public void ControlFlow_If_ToDisplayLine()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("If [ $x > 0 ]", step.ToDisplayLine());
    }

    [Fact]
    public void ControlFlow_EndIf_ToDisplayLine()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("End If", step.ToDisplayLine());
    }

    [Fact]
    public void SelfClosing_NewRecord_ToDisplayLine()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"7\" name=\"New Record/Request\"/>");
        var step = ScriptStep.FromXml(el);
        Assert.Equal("New Record/Request", step.ToDisplayLine());
    }

    [Fact]
    public void Disabled_Step_ToDisplayLine()
    {
        var el = MakeStep("<Step enable=\"False\" id=\"93\" name=\"Beep\"/>");
        var step = ScriptStep.FromXml(el);
        Assert.False(step.Enabled);
    }

    [Fact]
    public void UnknownStep_PreservesSourceXml()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"><Foo>bar</Foo></Step>");
        var step = ScriptStep.FromXml(el);
        Assert.Null(step.Definition);
        Assert.NotNull(step.SourceXml);
        // Display shows original name
        Assert.Contains("FutureStep", step.ToDisplayLine());
        // XML serializes as comment with original name preserved
        var xml = step.ToXml();
        Assert.Equal("# (comment)", xml.Attribute("name")?.Value);
        Assert.Contains("FutureStep", xml.Element("Text")?.Value ?? "");
    }

    [Fact]
    public void FromDisplayLine_SetVariable()
    {
        var step = ScriptStep.FromDisplayLine("Set Variable [ $x ; Value: 1 ]");
        Assert.Equal("Set Variable", step.Definition?.Name);
        var xml = step.ToXml();
        Assert.Equal("141", xml.Attribute("id")?.Value);
    }

    [Fact]
    public void FromDisplayLine_UnknownStep_HasNullDefinition()
    {
        var step = ScriptStep.FromDisplayLine("some random text");
        Assert.Null(step.Definition);
        // But ToXml emits it as a comment for safety
        var xml = step.ToXml();
        Assert.Equal("89", xml.Attribute("id")?.Value);
    }

    [Fact]
    public void Validate_UnknownStep_ProducesError()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"/>");
        var step = ScriptStep.FromXml(el);
        var diagnostics = step.Validate(0);
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
    }
}
