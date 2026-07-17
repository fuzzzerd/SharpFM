using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Regression coverage for <see cref="FmScript.Apply"/>'s param-map handling.
/// MCP / external callers send structured param dictionaries
/// (<c>{"Name": "$i"}</c>), which the apply path must translate into the
/// HR-token form each step's FromDisplay factory expects. Positional params
/// (no <c>HrLabel</c> in metadata) must arrive unprefixed, otherwise the
/// label leaks into the XML and breaks under FileMaker at runtime.
/// </summary>
public class FmScriptApplyTests
{
    private static FmScript EmptyScript() => new(new List<ScriptStep>());

    [Fact]
    public void ApplyAdd_SetVariable_PositionalNameDoesNotReceiveLabelPrefix()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$i", ["Value"] = "1" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<SetVariableStep>(script.Steps.Single());
        Assert.Equal("$i", step.Name);
        Assert.Equal("1", step.Value.Text);
    }

    [Fact]
    public void ApplyAdd_ExitLoopIf_PositionalConditionDoesNotReceiveLabelPrefix()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Exit Loop If",
            Params: new Dictionary<string, string?> { ["condition"] = "$i > $limit" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<ExitLoopIfStep>(script.Steps.Single());
        Assert.Equal("$i > $limit", step.Condition.Text);
    }

    [Fact]
    public void ApplyAdd_If_PositionalConditionDoesNotReceiveLabelPrefix()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "If",
            Params: new Dictionary<string, string?> { ["condition"] = "$x = 1" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<IfStep>(script.Steps.Single());
        Assert.Equal("$x = 1", step.Condition.Text);
    }

    [Fact]
    public void ApplyAdd_SetVariable_OutOfOrderParams_ProduceCorrectXml()
    {
        // Agent provides params in arbitrary order; metadata-driven ordering
        // ensures the HR token sequence matches what FromDisplay expects.
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Value"] = "100", ["Name"] = "$count" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<SetVariableStep>(script.Steps.Single());
        Assert.Equal("$count", step.Name);
        Assert.Equal("100", step.Value.Text);
    }

    [Fact]
    public void ApplyAdd_SetVariable_KeyMatchIsCaseInsensitive()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["name"] = "$lc", ["value"] = "5" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<SetVariableStep>(script.Steps.Single());
        Assert.Equal("$lc", step.Name);
        Assert.Equal("5", step.Value.Text);
    }

    [Fact]
    public void ApplyUpdate_SetVariable_PositionalNameDoesNotReceiveLabelPrefix()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$old", ["Value"] = "1" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Name"] = "$new" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<SetVariableStep>(script.Steps[0]);
        Assert.Equal("$new", step.Name);
    }

    [Fact]
    public void ApplyUpdate_PreservesParamsNotInMap()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$x", ["Value"] = "1" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Value"] = "2" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<SetVariableStep>(script.Steps[0]);
        Assert.Equal("$x", step.Name);
        Assert.Equal("2", step.Value.Text);
    }

    [Fact]
    public void ApplyUpdate_MutatesStepInstanceInPlace()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$x", ["Value"] = "1" }));
        var before = script.Steps[0];

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Value"] = "2" });

        Assert.Empty(script.Apply(update));

        Assert.Same(before, script.Steps[0]);
    }

    [Fact]
    public void ApplyUpdate_PreservesStateDisplayTextCannotCarry()
    {
        // Custom dialog buttons only exist in the XML; a Title-only update
        // must not reset them (or the Message) to defaults.
        var script = FmScript.FromXml("""
            <fmxmlsnippet type="FMObjectList">
              <Step enable="True" id="87" name="Show Custom Dialog">
                <Title><Calculation><![CDATA["Hi"]]></Calculation></Title>
                <Message><Calculation><![CDATA["Pick"]]></Calculation></Message>
                <Buttons>
                  <Button CommitState="True"><Calculation><![CDATA["Yes"]]></Calculation></Button>
                  <Button CommitState="False"><Calculation><![CDATA["No"]]></Calculation></Button>
                  <Button CommitState="False" />
                </Buttons>
              </Step>
            </fmxmlsnippet>
            """);

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Title"] = "\"Hello\"" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<ShowCustomDialogStep>(script.Steps[0]);
        Assert.Equal("\"Hello\"", step.Title.Text);
        Assert.Equal("\"Pick\"", step.Message.Text);
        Assert.Equal("\"Yes\"", step.Buttons[0].Label?.Text);
        Assert.Equal("\"No\"", step.Buttons[1].Label?.Text);
    }

    [Fact]
    public void ApplyUpdate_UnknownParam_ReturnsError()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$x", ["Value"] = "1" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Bogus"] = "x" });

        var errors = script.Apply(update);

        Assert.Contains(errors, e => e.Contains("Bogus"));
    }

    [Fact]
    public void ApplyAdd_UnknownParam_ReturnsErrorAndDoesNotInsert()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "If",
            Params: new Dictionary<string, string?> { ["NotAThing"] = "1" });

        var errors = script.Apply(op);

        Assert.Contains(errors, e => e.Contains("NotAThing"));
        Assert.Empty(script.Steps);
    }

    [Fact]
    public void ApplyAdd_BooleanParam_AcceptsOnOff()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Sort Records",
            Params: new Dictionary<string, string?> { ["With dialog"] = "On", ["Restore"] = "Off" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<SortRecordsStep>(script.Steps.Single());
        Assert.True(step.WithDialog);
        Assert.False(step.RestoreStoredOrder);
    }

    [Fact]
    public void ApplyUpdate_InvalidBooleanValue_ReturnsError()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Sort Records"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["With dialog"] = "Maybe" });

        var errors = script.Apply(update);

        Assert.Contains(errors, e => e.Contains("On") && e.Contains("Off"));
    }

    [Fact]
    public void ApplyUpdate_NullParamValue_ReturnsError()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Set Variable",
            Params: new Dictionary<string, string?> { ["Name"] = "$x", ["Value"] = "1" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Value"] = null });

        var errors = script.Apply(update);

        Assert.Contains(errors, e => e.Contains("Value"));
        var step = Assert.IsType<SetVariableStep>(script.Steps[0]);
        Assert.Equal("1", step.Value.Text);
    }

    [Fact]
    public void ApplyAdd_ShowCustomDialog_ButtonsParam_SetsButtons()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Show Custom Dialog",
            Params: new Dictionary<string, string?>
            {
                ["Title"] = "\"Confirm\"",
                ["Buttons"] = "[ \"Yes\" commit ; \"No\" nocommit ; \"\" nocommit ]",
            });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<ShowCustomDialogStep>(script.Steps.Single());
        Assert.Equal("\"Confirm\"", step.Title.Text);
        Assert.Equal(3, step.Buttons.Count);
        Assert.Equal("\"Yes\"", step.Buttons[0].Label?.Text);
        Assert.True(step.Buttons[0].CommitState);
        Assert.Equal("\"No\"", step.Buttons[1].Label?.Text);
        Assert.False(step.Buttons[1].CommitState);
    }

    [Fact]
    public void ApplyUpdate_ShowCustomDialog_ButtonsParam_ReplacesButtons()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Show Custom Dialog",
            Params: new Dictionary<string, string?> { ["Title"] = "\"T\"" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Buttons"] = "[ \"Go\" commit ]" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<ShowCustomDialogStep>(script.Steps[0]);
        Assert.Equal("\"T\"", step.Title.Text);
        var button = Assert.Single(step.Buttons);
        Assert.Equal("\"Go\"", button.Label?.Text);
        Assert.True(button.CommitState);
    }

    [Fact]
    public void ApplyUpdate_ShowCustomDialog_InputsParam_SetsInputFields()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Show Custom Dialog",
            Params: new Dictionary<string, string?> { ["Title"] = "\"T\"" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Inputs"] = "[ \"Age\" plain $age ]" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<ShowCustomDialogStep>(script.Steps[0]);
        Assert.Equal("\"T\"", step.Title.Text);
        var input = Assert.Single(step.InputFields!);
        Assert.Equal("Age", input.Label?.Text);
        Assert.Equal("$age", input.Target.ToDisplayString());
    }

    [Fact]
    public void ApplyAdd_PerformScript_ScriptParam_SetsByReferenceTarget()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Perform Script",
            Params: new Dictionary<string, string?> { ["Script"] = "\"Subroutine\" (#12)" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<PerformScriptStep>(script.Steps.Single());
        var byRef = Assert.IsType<PerformScriptTarget.ByReference>(step.Target);
        Assert.Equal("Subroutine", byRef.Script.Name);
        Assert.Equal(12, byRef.Script.Id);
    }

    [Fact]
    public void ApplyAdd_PerformScript_ByNameCalcParam_SetsCalculationTarget()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Perform Script",
            Params: new Dictionary<string, string?> { ["Target"] = "By name: Get ( ScriptName )" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<PerformScriptStep>(script.Steps.Single());
        var byCalc = Assert.IsType<PerformScriptTarget.ByCalculation>(step.Target);
        Assert.Equal("Get ( ScriptName )", byCalc.NameCalc.Text);
    }

    [Fact]
    public void ApplyUpdate_PerformScript_ParameterParam_SetsAndPreservesTarget()
    {
        // The Parameter slot binds to an emit-only wire projection; the
        // display-grammar route must reach the real Parameter property
        // without disturbing the script target.
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Perform Script",
            Params: new Dictionary<string, string?> { ["Script"] = "\"Subroutine\" (#12)" }));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Parameter"] = "42" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<PerformScriptStep>(script.Steps[0]);
        Assert.Equal("42", step.Parameter?.Text);
        var byRef = Assert.IsType<PerformScriptTarget.ByReference>(step.Target);
        Assert.Equal("Subroutine", byRef.Script.Name);
        Assert.Equal(12, byRef.Script.Id);
    }

    [Fact]
    public void ApplyUpdate_ConvertFile_PositionalEnumParam_SetsValue()
    {
        // Convert File's data source renders as an unlabeled positional token
        // and its parser takes the first match, so the applier must place the
        // new token ahead of the step's current display tokens.
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Convert File"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["DataSourceType"] = "XMLSource" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<ConvertFileStep>(script.Steps[0]);
        Assert.Equal("XMLSource", step.DataSourceType);
    }

    [Fact]
    public void ApplyUpdate_SameValueAgain_SucceedsWithoutChange()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(
            Action: "add",
            StepName: "Go to Layout",
            Params: new Dictionary<string, string?> { ["Layout"] = "\"Detail\" (#7)" }));
        var xmlBefore = script.Steps[0].ToXml().ToString();

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Layout"] = "\"Detail\" (#7)" });

        Assert.Empty(script.Apply(update));
        Assert.Equal(xmlBefore, script.Steps[0].ToXml().ToString());
    }

    [Fact]
    public void ApplyUpdate_ValueTheStepCannotExpress_ReturnsErrorWithoutChange()
    {
        // Dial Phone's use-dial-preferences flag has no addressable display
        // token; the applier must reject it rather than write the raw display
        // value into the wire-facing property.
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Dial Phone"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["UseDialPreferences"] = "On" });

        var errors = script.Apply(update);

        Assert.NotEmpty(errors);
        var step = Assert.IsType<DialPhoneStep>(script.Steps[0]);
        Assert.False(step.UseDialPreferences);
    }

    [Fact]
    public void ApplyUpdate_SortRecords_SortListXmlFragment_SetsSortOrder()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Sort Records"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?>
            {
                ["Sort"] = """<SortList Maintain="True" value="True"><Sort type="Ascending"><PrimaryField><Field table="Customer" id="1" name="name" /></PrimaryField></Sort></SortList>""",
            });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<SortRecordsStep>(script.Steps[0]);
        Assert.NotNull(step.Sort);
        Assert.Contains("PrimaryField", step.ToXml().ToString());
    }

    [Fact]
    public void ApplyUpdate_PerformFind_QueryXmlFragment_ReplacesQuery()
    {
        // Perform Find's query lives in the passthrough bag behind an HrOnly
        // slot; a second fragment must replace the first, not accumulate.
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Perform Find"));

        var first = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?>
            {
                ["Query"] = """<Query><RequestRow operation="Include"><Criteria><Field table="Customer" id="1" name="state" /><Text>==CA</Text></Criteria></RequestRow></Query>""",
            });
        Assert.Empty(script.Apply(first));

        var second = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?>
            {
                ["Query"] = """<Query><RequestRow operation="Include"><Criteria><Field table="Customer" id="1" name="state" /><Text>==NY</Text></Criteria></RequestRow></Query>""",
            });
        Assert.Empty(script.Apply(second));

        var xml = script.Steps[0].ToXml().ToString();
        Assert.Contains("==NY", xml);
        Assert.DoesNotContain("==CA", xml);
    }

    [Fact]
    public void ApplyUpdate_XmlFragmentWithWrongRootElement_ReturnsError()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Sort Records"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Sort"] = "<Query />" });

        var errors = script.Apply(update);

        Assert.Contains(errors, e => e.Contains("SortList"));
    }

    [Fact]
    public void ApplyUpdate_MalformedXmlFragment_ReturnsError()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Sort Records"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Sort"] = "<SortList><Sort></SortList>" });

        var errors = script.Apply(update);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ApplyAdd_GoToLayout_NamedLayoutParam_SetsTarget()
    {
        var script = EmptyScript();

        var op = new ScriptStepOperation(
            Action: "add",
            StepName: "Go to Layout",
            Params: new Dictionary<string, string?> { ["Layout"] = "\"Detail\" (#7)" });

        Assert.Empty(script.Apply(op));

        var step = Assert.IsType<GoToLayoutStep>(script.Steps.Single());
        var named = Assert.IsType<LayoutTarget.Named>(step.Target);
        Assert.Equal("Detail", named.Layout.Name);
        Assert.Equal(7, named.Layout.Id);
    }

    [Fact]
    public void ApplyUpdate_GoToLayout_LayoutNameCalcParam_SetsTarget()
    {
        var script = EmptyScript();
        script.Apply(new ScriptStepOperation(Action: "add", StepName: "Go to Layout"));

        var update = new ScriptStepOperation(
            Action: "update",
            Index: 0,
            Params: new Dictionary<string, string?> { ["Layout"] = "Layout Name: $layoutName" });

        Assert.Empty(script.Apply(update));

        var step = Assert.IsType<GoToLayoutStep>(script.Steps[0]);
        var byName = Assert.IsType<LayoutTarget.ByNameCalc>(step.Target);
        Assert.Equal("$layoutName", byName.Calc.Text);
    }
}
