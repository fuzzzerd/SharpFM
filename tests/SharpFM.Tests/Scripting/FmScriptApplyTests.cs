using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
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
}
