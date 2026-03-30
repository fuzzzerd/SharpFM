using System.Linq;
using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class CompletionProviderTests
{
    [Fact]
    public void EmptyLine_SuggestsStepNames()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions("", 0);
        Assert.Equal(CompletionContext.StepName, context);
        Assert.True(items.Count > 100);
        Assert.Contains(items, i => i.Text == "Set Variable");
        Assert.Contains(items, i => i.Text == "If");
    }

    [Fact]
    public void PartialStepName_SuggestsMatches()
    {
        var (context, items) = FmScriptCompletionProvider.GetCompletions("Set", 3);
        Assert.Equal(CompletionContext.StepName, context);
        Assert.Contains(items, i => i.Text == "Set Variable");
        Assert.Contains(items, i => i.Text == "Set Field");
    }

    [Fact]
    public void InsideBrackets_SuggestsParamLabels()
    {
        // Add Account has labeled params: Authenticate via, Account Name, etc.
        var (context, items) = FmScriptCompletionProvider.GetCompletions("Add Account [ ", 14);
        Assert.Equal(CompletionContext.ParamLabel, context);
        Assert.Contains(items, i => i.Text.StartsWith("Authenticate via"));
    }

    [Fact]
    public void UnlabeledBooleanParam_SuggestsOnOff()
    {
        // Allow User Abort has an unlabeled boolean param → should suggest On/Off
        var (context, items) = FmScriptCompletionProvider.GetCompletions(
            "Allow User Abort [ ", 19);
        Assert.Equal(CompletionContext.ParamValue, context);
        Assert.Contains(items, i => i.Text == "On");
        Assert.Contains(items, i => i.Text == "Off");
    }

    [Fact]
    public void GoToRecord_SuggestsPositionalEnumValues()
    {
        // Go to Record/Request/Page has RowPageLocation enum as first unlabeled param
        var (context, items) = FmScriptCompletionProvider.GetCompletions(
            "Go to Record/Request/Page [ ", 28);
        Assert.Equal(CompletionContext.ParamValue, context);
        Assert.Contains(items, i => i.Text == "First");
        Assert.Contains(items, i => i.Text == "Next");
        Assert.Contains(items, i => i.Text == "Last");
        // Should also include labeled params like "Exit after last:"
        Assert.Contains(items, i => i.Text.StartsWith("Exit after last"));
    }

    [Fact]
    public void GoToRecord_AfterFirstParam_SuggestsLabels()
    {
        // After providing the first positional param, suggest remaining labeled params
        var (context, items) = FmScriptCompletionProvider.GetCompletions(
            "Go to Record/Request/Page [ Next ; ", 35);
        Assert.Contains(items, i => i.Text.StartsWith("Exit after last"));
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
        Assert.True(items.Count > 100);
    }
}
