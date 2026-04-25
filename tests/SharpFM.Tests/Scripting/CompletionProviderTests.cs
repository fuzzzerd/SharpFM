using System.Linq;
using SharpFM.Scripting;
using SharpFM.Scripting.Editor;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

/// <summary>
/// Tests for <see cref="FmScriptCompletionProvider"/>, which now reads
/// exclusively from <c>StepRegistry</c>. Only step names with a typed
/// POCO appear in completions — during the pilot that's three steps
/// (Beep, Set Error Capture, If). These tests are written against that
/// constraint and are expected to grow as more POCOs arrive during the
/// sweep phase.
/// </summary>
public class CompletionProviderTests
{
    [Fact]
    public void EmptyLine_SuggestsPocoStepNames()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions("", 0);
        Assert.Equal(CompletionContext.StepName, context);
        Assert.Contains(items, i => i.Text == "Beep");
        Assert.Contains(items, i => i.Text == "Set Error Capture");
        Assert.Contains(items, i => i.Text == "If");
    }

    [Fact]
    public void AllSweepPhaseStepsSuggested()
    {
        // With the POCO sweep complete, formerly-absent steps like
        // Import Records and Go to Record/Request/Page should now appear.
        var (_, items) = FmScriptCompletionProvider.GetCompletions("", 0);
        Assert.Contains(items, i => i.Text == "Import Records");
        Assert.Contains(items, i => i.Text == "Go to Record/Request/Page");
        Assert.Contains(items, i => i.Text == "Send Mail");
    }

    [Fact]
    public void PartialStepName_SuggestsMatches()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions("Set", 3);
        Assert.Equal(CompletionContext.StepName, context);
        Assert.Contains(items, i => i.Text == "Set Error Capture");
    }

    [Fact]
    public void InsideBrackets_NoLabeledParams_PilotRegression()
    {
        // None of the three pilot POCOs expose HrLabels, so label-based
        // completion returns nothing. This test codifies the regression
        // so it's obvious when later POCOs add labeled-param coverage.
        var (context, _) = FmScriptCompletionProvider.GetCompletions("Set Error Capture [ ", 20);
        // No labels means positional value path hits ValidValues for the
        // boolean Set param, which is Context=ParamValue.
        Assert.Equal(CompletionContext.ParamValue, context);
    }

    [Fact]
    public void UnlabeledBooleanParam_SuggestsOnOff()
    {
        // Set Error Capture has a single unlabeled boolean — On/Off
        // should appear as positional value suggestions.
        var (context, items) = FmScriptCompletionProvider.GetCompletions(
            "Set Error Capture [ ", 20);
        Assert.Equal(CompletionContext.ParamValue, context);
        Assert.Contains(items, i => i.Text == "On");
        Assert.Contains(items, i => i.Text == "Off");
    }

    [Fact]
    public void UnknownStep_ReturnsNone()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions(
            "FakeStep [ ", 11);
        Assert.Equal(CompletionContext.None, context);
        Assert.Empty(items);
    }

    [Fact]
    public void IndentedLine_SuggestsStepNames()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions("    ", 4);
        Assert.Equal(CompletionContext.StepName, context);
        Assert.NotEmpty(items);
    }

    [Fact]
    public void StepNameCompletion_ReturnsPocoStepsFromRegistry()
    {
        var (_, items) = FmScriptCompletionProvider.GetCompletions("", 0);
        var names = items.Select(i => i.Text).ToList();
        Assert.Contains("Beep", names);
        Assert.Contains("Set Error Capture", names);
        Assert.Contains("If", names);
        // Sweep complete: Import Records is now a migrated POCO.
        Assert.Contains("Import Records", names);
    }

    [Fact]
    public void ParamValueCompletion_UsesValidValuesFromParamMetadata()
    {
        // Confirms the completion provider reads ValidValues off
        // ParamMetadata rather than the legacy StepParam shape.
        var (_, items) = FmScriptCompletionProvider.GetCompletions(
            "Set Error Capture [ ", 20);
        Assert.Contains(items, i => i.Text == "On");
        Assert.Contains(items, i => i.Text == "Off");
    }

    [Fact]
    public void StepNameCompletion_MultiParamStep_InsertsBracketedSnippet()
    {
        // Regression: accepting the step-name completion used to insert
        // the bare name ("Set Error Capture"), so when the param-value
        // prompt fired next, the user ended up with "Set Error Capture On"
        // instead of "Set Error Capture [ On ]". Fix: synthesize a
        // Monaco snippet with bracketed placeholder tab-stops.
        var (_, items) = FmScriptCompletionProvider.GetCompletions("Set E", 5);
        var data = (SharpFM.Scripting.Editor.FmScriptCompletionData)
            items.Single(i => i.Text == "Set Error Capture");

        // The snippet field is the text actually inserted on accept. It
        // must contain the bracketed form with a Monaco ${N:placeholder}
        // tab-stop so the first value is pre-selected for editing.
        var snippetField = typeof(SharpFM.Scripting.Editor.FmScriptCompletionData)
            .GetField("_snippet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var snippet = (string?)snippetField!.GetValue(data);

        Assert.NotNull(snippet);
        Assert.Contains("[", snippet);
        Assert.Contains("]", snippet);
        Assert.Contains("${1:", snippet);
    }

    [Fact]
    public void StepNameCompletion_ZeroParamStep_InsertsBareName()
    {
        // Beep has no params — the snippet is just the name, no brackets.
        var (_, items) = FmScriptCompletionProvider.GetCompletions("Bee", 3);
        var data = (SharpFM.Scripting.Editor.FmScriptCompletionData)
            items.Single(i => i.Text == "Beep");

        var snippetField = typeof(SharpFM.Scripting.Editor.FmScriptCompletionData)
            .GetField("_snippet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var snippet = (string?)snippetField!.GetValue(data);

        Assert.Equal("Beep", snippet);
    }
}
