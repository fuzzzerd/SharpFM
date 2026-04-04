using System.Xml.Linq;
using SharpFM.Scripting;
using SharpFM.Scripting.Model;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class FmScriptMutationTests
{
    private static FmScript MakeScript(params string[] displayLines)
    {
        var text = string.Join("\n", displayLines);
        return FmScript.FromDisplayText(text);
    }

    private static ScriptStep MakeStep(string displayLine)
    {
        return ScriptStep.FromDisplayLine(displayLine);
    }

    // --- AddStep ---

    [Fact]
    public void AddStep_InsertsAtIndex()
    {
        var script = MakeScript("Beep", "Halt");
        var step = MakeStep("# inserted");

        script.AddStep(1, step);

        Assert.Equal(3, script.StepCount);
        Assert.Contains("inserted", script.GetStep(1).ToDisplayLine());
    }

    [Fact]
    public void AddStep_FiresStepsChanged()
    {
        var script = MakeScript("Beep");
        var fired = false;
        script.StepsChanged += (_, _) => fired = true;

        script.AddStep(0, MakeStep("Halt"));

        Assert.True(fired);
    }

    // --- RemoveStep ---

    [Fact]
    public void RemoveStep_RemovesAtIndex()
    {
        var script = MakeScript("Beep", "# middle", "Halt");

        script.RemoveStep(1);

        Assert.Equal(2, script.StepCount);
        Assert.Contains("Halt", script.GetStep(1).ToDisplayLine());
    }

    [Fact]
    public void RemoveStep_FiresStepsChanged()
    {
        var script = MakeScript("Beep", "Halt");
        var fired = false;
        script.StepsChanged += (_, _) => fired = true;

        script.RemoveStep(0);

        Assert.True(fired);
    }

    // --- MoveStep ---

    [Fact]
    public void MoveStep_ReordersSteps()
    {
        var script = MakeScript("Beep", "# middle", "Halt");

        script.MoveStep(2, 0);

        Assert.Contains("Halt", script.GetStep(0).ToDisplayLine());
        Assert.Contains("Beep", script.GetStep(1).ToDisplayLine());
    }

    // --- UpdateStep ---

    [Fact]
    public void UpdateStep_ReplacesStep()
    {
        var script = MakeScript("Beep");
        var replacement = MakeStep("Halt");

        script.UpdateStep(0, replacement);

        Assert.Contains("Halt", script.GetStep(0).ToDisplayLine());
    }

    [Fact]
    public void UpdateStep_FromDisplayLine()
    {
        var script = MakeScript("Beep");

        script.UpdateStep(0, "# from display");

        Assert.Contains("from display", script.GetStep(0).ToDisplayLine());
    }

    // --- ReplaceSteps ---

    [Fact]
    public void ReplaceSteps_ClearsAndSetsNew()
    {
        var script = MakeScript("Beep", "# b", "Halt");

        script.ReplaceSteps([MakeStep("# x"), MakeStep("Beep")]);

        Assert.Equal(2, script.StepCount);
    }

    [Fact]
    public void ReplaceSteps_FiresStepsChanged()
    {
        var script = MakeScript("Beep");
        var fired = false;
        script.StepsChanged += (_, _) => fired = true;

        script.ReplaceSteps([MakeStep("Halt")]);

        Assert.True(fired);
    }

    // --- FindSteps ---

    [Fact]
    public void FindSteps_ByName()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step>"
            + "<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>b</Text></Step>"
            + "</fmxmlsnippet>";
        var script = FmScript.FromXml(xml);

        var comments = script.FindSteps("# (comment)");

        Assert.Equal(2, comments.Count);
        Assert.Equal(0, comments[0].Index);
        Assert.Equal(2, comments[1].Index);
    }

    [Fact]
    public void FindSteps_CaseInsensitive()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"
            + "</fmxmlsnippet>";
        var script = FmScript.FromXml(xml);

        var results = script.FindSteps("beep");

        Assert.Single(results);
    }

    [Fact]
    public void FindSteps_NoMatches_ReturnsEmpty()
    {
        var script = MakeScript("# comment");

        var results = script.FindSteps("Set Variable");

        Assert.Empty(results);
    }

    // --- GetStep / StepCount ---

    [Fact]
    public void StepCount_ReflectsCurrentState()
    {
        var script = MakeScript("Beep", "Halt");
        Assert.Equal(2, script.StepCount);

        script.RemoveStep(0);
        Assert.Equal(1, script.StepCount);
    }

    // --- Roundtrip after mutation ---

    [Fact]
    public void Mutation_ThenToXml_Roundtrips()
    {
        var script = MakeScript("# original");
        script.AddStep(1, MakeStep("Beep"));

        var xml = script.ToXml();
        Assert.Contains("original", xml);
        Assert.Contains("Beep", xml);
    }

    [Fact]
    public void Mutation_ThenToDisplayText_Roundtrips()
    {
        var script = MakeScript("Beep");
        script.AddStep(1, MakeStep("Halt"));

        var text = script.ToDisplayText();
        Assert.Contains("Beep", text);
        Assert.Contains("Halt", text);
    }
}
