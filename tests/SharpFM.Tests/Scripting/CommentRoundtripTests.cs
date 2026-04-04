using SharpFM.Editors;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class CommentRoundtripTests
{
    private static string Wrap(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    [Fact]
    public void TwoSeparateComments_StaySeparate_AfterRoundtrip()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>first</Text></Step>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>second</Text></Step>");

        var script = FmScript.FromXml(xml);
        Assert.Equal(2, script.Steps.Count);

        // Roundtrip through display text
        var displayText = script.ToDisplayText();
        var reparsed = FmScript.FromDisplayText(displayText);

        Assert.Equal(2, reparsed.Steps.Count);
        Assert.Contains("first", reparsed.Steps[0].ToDisplayLine());
        Assert.Contains("second", reparsed.Steps[1].ToDisplayLine());
    }

    [Fact]
    public void MultilineComment_DisplaysTruncated()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>line one\nline two</Text></Step>");

        var script = FmScript.FromXml(xml);
        Assert.Single(script.Steps);

        // Display should show truncated first line with ellipsis
        var displayText = script.ToDisplayText();
        Assert.Contains("line one", displayText);
        Assert.Contains("\u2026", displayText); // ellipsis
        Assert.DoesNotContain("line two", displayText); // not shown in text
    }

    [Fact]
    public void MultilineComment_PreservedOnSave_WhenUnchanged()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>line one\nline two</Text></Step>");

        var editor = new ScriptClipEditor(xml);
        editor.Save(); // save without editing

        var savedXml = editor.ToXml();
        Assert.Contains("line one", savedXml);
        Assert.Contains("line two", savedXml); // full content preserved
    }

    [Fact]
    public void MultilineComment_ThenSeparateComment_PreservesBoth()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>multi\nline</Text></Step>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>separate</Text></Step>");

        var editor = new ScriptClipEditor(xml);
        editor.Save(); // save without editing

        var savedXml = editor.ToXml();
        Assert.Contains("multi", savedXml);
        Assert.Contains("line", savedXml);
        Assert.Contains("separate", savedXml);
        Assert.Equal(2, editor.Script.Steps.Count);
    }

    [Fact]
    public void CommentBetweenSteps_PreservesStepCount()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>note</Text></Step>"
            + "<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");

        var script = FmScript.FromXml(xml);
        Assert.Equal(3, script.Steps.Count);

        var displayText = script.ToDisplayText();
        var reparsed = FmScript.FromDisplayText(displayText);
        Assert.Equal(3, reparsed.Steps.Count);
    }
}
