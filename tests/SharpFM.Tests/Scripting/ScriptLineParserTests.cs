using System;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class ScriptLineParserTests
{
    [Fact]
    public void ParsesSimpleStep()
    {
        var line = ScriptLineParser.ParseLine("Beep");
        Assert.Equal("Beep", line.StepName);
        Assert.Empty(line.Params);
        Assert.False(line.Disabled);
        Assert.False(line.IsComment);
    }

    [Fact]
    public void ParsesStepWithParams()
    {
        var line = ScriptLineParser.ParseLine("Set Variable [ $x ; Value: 1 ]");
        Assert.Equal("Set Variable", line.StepName);
        Assert.Equal(2, line.Params.Length);
        Assert.Equal("$x", line.Params[0]);
        Assert.Equal("Value: 1", line.Params[1]);
    }

    [Fact]
    public void ParsesCommentLine()
    {
        var line = ScriptLineParser.ParseLine("# hello world");
        Assert.Equal("# (comment)", line.StepName);
        Assert.True(line.IsComment);
        Assert.Single(line.Params);
        Assert.Equal("hello world", line.Params[0]);
    }

    [Fact]
    public void ParsesDisabledStep()
    {
        var line = ScriptLineParser.ParseLine("// If [ $x > 1 ]");
        Assert.True(line.Disabled);
        Assert.Equal("If", line.StepName);
        Assert.Single(line.Params);
        Assert.Equal("$x > 1", line.Params[0]);
    }

    [Fact]
    public void ParsesNestedParens()
    {
        var line = ScriptLineParser.ParseLine("Set Variable [ $x ; Value: GetValue ( $list ; 1 ) ]");
        Assert.Equal("Set Variable", line.StepName);
        Assert.Equal(2, line.Params.Length);
        Assert.Equal("$x", line.Params[0]);
        Assert.Equal("Value: GetValue ( $list ; 1 )", line.Params[1]);
    }

    [Fact]
    public void ParsesQuotedSemicolons()
    {
        var line = ScriptLineParser.ParseLine("Set Field [ Table::F ; \"a ; b\" ]");
        Assert.Equal("Set Field", line.StepName);
        Assert.Equal(2, line.Params.Length);
        Assert.Equal("Table::F", line.Params[0]);
        Assert.Equal("\"a ; b\"", line.Params[1]);
    }

    [Fact]
    public void ParsesEmptyBrackets()
    {
        var line = ScriptLineParser.ParseLine("Commit Records/Requests [ ]");
        Assert.Equal("Commit Records/Requests", line.StepName);
        Assert.Empty(line.Params);
    }

    [Fact]
    public void ParsesStepWithNoParams()
    {
        var line = ScriptLineParser.ParseLine("End If");
        Assert.Equal("End If", line.StepName);
        Assert.Empty(line.Params);
        Assert.False(line.Disabled);
    }

    [Fact]
    public void HandlesLeadingWhitespace()
    {
        var line = ScriptLineParser.ParseLine("        Set Variable [ $x ; Value: 1 ]");
        Assert.Equal("Set Variable", line.StepName);
        Assert.Equal(2, line.Params.Length);
    }

    [Fact]
    public void ParsesMultipleLines()
    {
        var input = "# comment\nSet Variable [ $x ; Value: 1 ]\nEnd If";
        var lines = ScriptLineParser.Parse(input);
        Assert.Equal(3, lines.Count);
        Assert.True(lines[0].IsComment);
        Assert.Equal("Set Variable", lines[1].StepName);
        Assert.Equal("End If", lines[2].StepName);
    }

    [Fact]
    public void SkipsBlankLines()
    {
        var input = "# comment\n\n\nEnd If";
        var lines = ScriptLineParser.Parse(input);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void ParsesEmptyInput()
    {
        var lines = ScriptLineParser.Parse("");
        Assert.Empty(lines);
    }

    [Fact]
    public void ParsesDisabledComment()
    {
        var line = ScriptLineParser.ParseLine("// # disabled comment");
        Assert.True(line.Disabled);
        Assert.True(line.IsComment);
        Assert.Equal("disabled comment", line.Params[0]);
    }

    [Fact]
    public void MergesMultilineCalculation()
    {
        var input = "Set Variable [ $result ; Value: Let (\n  x = 1 ;\n  x + 1\n) ]";
        var lines = ScriptLineParser.Parse(input);
        Assert.Single(lines);
        Assert.Equal("Set Variable", lines[0].StepName);
        Assert.Equal(2, lines[0].Params.Length);
        Assert.Contains("Let (", lines[0].Params[1]);
    }

    [Fact]
    public void MergesMultilineWithFollowingStep()
    {
        var input = "Set Variable [ $x ; Value: Let (\n  a = 1 ;\n  a\n) ]\nBeep";
        var lines = ScriptLineParser.Parse(input);
        Assert.Equal(2, lines.Count);
        Assert.Equal("Set Variable", lines[0].StepName);
        Assert.Equal("Beep", lines[1].StepName);
    }

    [Fact]
    public void DoesNotMergeBalancedLines()
    {
        var input = "Set Variable [ $x ; Value: 1 ]\nBeep";
        var lines = ScriptLineParser.Parse(input);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void HasUnbalancedBrackets_Detects()
    {
        Assert.True(ScriptLineParser.HasUnbalancedBrackets("Set Variable [ $x ; Value: Let ("));
        Assert.False(ScriptLineParser.HasUnbalancedBrackets("Set Variable [ $x ; Value: 1 ]"));
        Assert.False(ScriptLineParser.HasUnbalancedBrackets("Beep"));
    }
}
