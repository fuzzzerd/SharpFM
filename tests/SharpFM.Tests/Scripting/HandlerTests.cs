using System.Xml.Linq;
using SharpFM.Scripting;
using SharpFM.Scripting.Handlers;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class HandlerTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    // --- SetFieldHandler ---

    [Fact]
    public void SetField_Display_TableAndCalc()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[$count + 1]]></Calculation>"
            + "<Field table=\"People\" id=\"1\" name=\"FirstName\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Set Field [ People::FirstName ; $count + 1 ]", step.ToDisplayLine());
    }

    [Fact]
    public void SetField_Display_NoParams()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"76\" name=\"Set Field\"></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Set Field", step.ToDisplayLine());
    }

    [Fact]
    public void SetField_Display_FieldNameOnly()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Field table=\"\" id=\"0\" name=\"MyField\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Set Field [ MyField ]", step.ToDisplayLine());
    }

    [Fact]
    public void SetField_BuildXml_FromParams()
    {
        var handler = new SetFieldHandler();
        var def = new StepDefinition { Name = "Set Field" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["People::FirstName", "$count + 1"]);

        Assert.NotNull(xml);
        Assert.Equal("People", xml!.Element("Field")!.Attribute("table")!.Value);
        Assert.Equal("FirstName", xml.Element("Field")!.Attribute("name")!.Value);
        Assert.Contains("$count + 1", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void SetField_BuildXml_Disabled()
    {
        var handler = new SetFieldHandler();
        var def = new StepDefinition { Name = "Set Field" };
        var xml = handler.BuildXmlFromDisplay(def, false, ["T::F", "1"]);

        Assert.Equal("False", xml!.Attribute("enable")!.Value);
    }

    // --- SetVariableHandler ---

    [Fact]
    public void SetVariable_BuildXml_FromParams()
    {
        var handler = new SetVariableHandler();
        var def = new StepDefinition { Name = "Set Variable" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["$count", "Value: $count + 1"]);

        Assert.Equal("$count", xml!.Element("Name")!.Value);
        Assert.Contains("$count + 1", xml.Element("Value")!.Element("Calculation")!.Value);
    }

    [Fact]
    public void SetVariable_BuildXml_WithRepetition()
    {
        var handler = new SetVariableHandler();
        var def = new StepDefinition { Name = "Set Variable" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["$arr[3]", "Value: \"x\""]);

        Assert.Equal("$arr", xml!.Element("Name")!.Value);
        Assert.Contains("3", xml.Element("Repetition")!.Element("Calculation")!.Value);
    }

    [Fact]
    public void SetVariable_ParseVarRepetition_Simple()
    {
        var (name, rep) = SetVariableHandler.ParseVarRepetition("$myVar");
        Assert.Equal("$myVar", name);
        Assert.Equal("1", rep);
    }

    [Fact]
    public void SetVariable_ParseVarRepetition_WithIndex()
    {
        var (name, rep) = SetVariableHandler.ParseVarRepetition("$arr[5]");
        Assert.Equal("$arr", name);
        Assert.Equal("5", rep);
    }

    // --- PerformScriptHandler ---

    [Fact]
    public void PerformScript_Display_ScriptNameAndParam()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Calculation><![CDATA[Get(ScriptParameter)]]></Calculation>"
            + "<Script id=\"1\" name=\"Initialize\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Perform Script [ \"Initialize\" ; Parameter: Get(ScriptParameter) ]", step.ToDisplayLine());
    }

    [Fact]
    public void PerformScript_Display_NoParams()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"1\" name=\"Perform Script\"></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Perform Script", step.ToDisplayLine());
    }

    [Fact]
    public void PerformScript_BuildXml_FromParams()
    {
        var handler = new PerformScriptHandler();
        var def = new StepDefinition { Name = "Perform Script" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["\"Initialize\"", "Parameter: Get(ScriptParameter)"]);

        Assert.Equal("Initialize", xml!.Element("Script")!.Attribute("name")!.Value);
        Assert.Contains("Get(ScriptParameter)", xml.Element("Calculation")!.Value);
    }

    // --- GoToLayoutHandler ---

    [Fact]
    public void GoToLayout_Display_NamedLayout()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"1\" name=\"Detail\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Layout [ \"Detail\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToLayout_Display_OriginalLayout()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"OriginalLayout\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Layout [ original layout ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToLayout_Display_ByCalculation()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"LayoutNameByCalculation\"/>"
            + "<Calculation><![CDATA[$layoutName]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Layout [ $layoutName ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToLayout_Display_WithAnimation()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"1\" name=\"Detail\"/>"
            + "<Animation value=\"SlideLeft\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Layout [ \"Detail\" ; Animation: SlideLeft ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToLayout_BuildXml_OriginalLayout()
    {
        var handler = new GoToLayoutHandler();
        var def = new StepDefinition { Name = "Go to Layout" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["original layout"]);

        Assert.Equal("OriginalLayout", xml!.Element("LayoutDestination")!.Attribute("value")!.Value);
        Assert.Null(xml.Element("Layout"));
    }

    [Fact]
    public void GoToLayout_BuildXml_NamedLayout()
    {
        var handler = new GoToLayoutHandler();
        var def = new StepDefinition { Name = "Go to Layout" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["\"Detail\""]);

        Assert.Equal("SelectedLayout", xml!.Element("LayoutDestination")!.Attribute("value")!.Value);
        Assert.Equal("Detail", xml.Element("Layout")!.Attribute("name")!.Value);
    }

    // --- GoToRecordHandler ---

    [Fact]
    public void GoToRecord_Display_Next()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + "<RowPageLocation value=\"Next\"/>"
            + "<Exit state=\"False\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Record/Request/Page [ Next ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToRecord_Display_WithExitAfterLast()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + "<RowPageLocation value=\"Next\"/>"
            + "<Exit state=\"True\"/></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Record/Request/Page [ Next ; Exit after last: On ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToRecord_Display_ByCalculation()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + "<RowPageLocation value=\"By Calculation\"/>"
            + "<Exit state=\"False\"/>"
            + "<Calculation><![CDATA[$recNum]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Go to Record/Request/Page [ By Calculation: $recNum ]", step.ToDisplayLine());
    }

    [Fact]
    public void GoToRecord_BuildXml_First()
    {
        var handler = new GoToRecordHandler();
        var def = new StepDefinition { Name = "Go to Record/Request/Page" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["First"]);

        Assert.Equal("First", xml!.Element("RowPageLocation")!.Attribute("value")!.Value);
    }

    [Fact]
    public void GoToRecord_BuildXml_ExitAfterLast()
    {
        var handler = new GoToRecordHandler();
        var def = new StepDefinition { Name = "Go to Record/Request/Page" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["Next", "Exit after last: On"]);

        Assert.Equal("True", xml!.Element("Exit")!.Attribute("state")!.Value);
    }

    // --- CommentHandler ---

    [Fact]
    public void Comment_Display_SingleLine()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello</Text></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("# hello", step.ToDisplayLine());
    }

    [Fact]
    public void Comment_ToXml_Roundtrip()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>test comment</Text></Step>");
        var step = ScriptStep.FromXml(el);
        var xml = step.ToXml();

        Assert.Equal("test comment", xml.Element("Text")!.Value);
    }

    // --- ControlFlowHandler ---

    [Fact]
    public void If_Display_WithCondition()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("If [ $x > 0 ]", step.ToDisplayLine());
    }

    [Fact]
    public void If_Display_NoCondition()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"68\" name=\"If\"></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("If", step.ToDisplayLine());
    }

    [Fact]
    public void ElseIf_Display_WithCondition()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"125\" name=\"Else If\">"
            + "<Calculation><![CDATA[$x < 10]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Else If [ $x < 10 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Else_Display()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"69\" name=\"Else\"/>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Else", step.ToDisplayLine());
    }

    [Fact]
    public void EndIf_Display()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("End If", step.ToDisplayLine());
    }

    [Fact]
    public void Loop_Display()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"71\" name=\"Loop\"/>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Loop", step.ToDisplayLine());
    }

    [Fact]
    public void EndLoop_Display()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"73\" name=\"End Loop\"/>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("End Loop", step.ToDisplayLine());
    }

    [Fact]
    public void ExitLoopIf_Display_WithCondition()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"72\" name=\"Exit Loop If\">"
            + "<Calculation><![CDATA[$done]]></Calculation></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Exit Loop If [ $done ]", step.ToDisplayLine());
    }

    [Fact]
    public void ControlFlow_BuildXml_If()
    {
        var handler = new ControlFlowHandler();
        var def = new StepDefinition { Name = "If" };
        var xml = handler.BuildXmlFromDisplay(def, true, ["$x > 0"]);

        Assert.Equal("68", xml!.Attribute("id")!.Value);
        Assert.Contains("$x > 0", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void ControlFlow_BuildXml_Else()
    {
        var handler = new ControlFlowHandler();
        var def = new StepDefinition { Name = "Else" };
        var xml = handler.BuildXmlFromDisplay(def, true, []);

        Assert.Equal("69", xml!.Attribute("id")!.Value);
    }

    [Fact]
    public void ControlFlow_BuildXml_EndIf()
    {
        var handler = new ControlFlowHandler();
        var def = new StepDefinition { Name = "End If" };
        var xml = handler.BuildXmlFromDisplay(def, true, []);

        Assert.Equal("70", xml!.Attribute("id")!.Value);
    }

    // --- ShowCustomDialogHandler ---

    [Fact]
    public void ShowCustomDialog_Display_AllParts()
    {
        var el = MakeStep(
            "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
            + "<Title><Calculation><![CDATA[\"Warning\"]]></Calculation></Title>"
            + "<Message><Calculation><![CDATA[\"Are you sure?\"]]></Calculation></Message>"
            + "<Buttons><Button><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
            + "<Button><Calculation><![CDATA[\"Cancel\"]]></Calculation></Button></Buttons></Step>");
        var step = ScriptStep.FromXml(el);

        var display = step.ToDisplayLine();
        Assert.Contains("Title: \"Warning\"", display);
        Assert.Contains("Message: \"Are you sure?\"", display);
        Assert.Contains("Buttons: \"OK\", \"Cancel\"", display);
    }

    [Fact]
    public void ShowCustomDialog_Display_NoParams()
    {
        var el = MakeStep("<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\"></Step>");
        var step = ScriptStep.FromXml(el);

        Assert.Equal("Show Custom Dialog", step.ToDisplayLine());
    }

    [Fact]
    public void ShowCustomDialog_BuildXml_FromParams()
    {
        var handler = new ShowCustomDialogHandler();
        var def = new StepDefinition { Name = "Show Custom Dialog" };
        var xml = handler.BuildXmlFromDisplay(def, true,
            ["Title: \"Warning\"", "Message: \"Sure?\"", "Buttons: OK, Cancel"]);

        Assert.Contains("\"Warning\"", xml!.Element("Title")!.Element("Calculation")!.Value);
        Assert.Contains("\"Sure?\"", xml.Element("Message")!.Element("Calculation")!.Value);
        Assert.Equal(2, xml.Element("Buttons")!.Elements("Button").Count());
    }
}
