using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Pure-function coverage for the ranges helper that drives the
/// continuation rail and step-index margin.
/// </summary>
public class MultiLineStatementRangesTests
{
    [Fact]
    public void Compute_SingleLineSteps_OneRangePerLine()
    {
        var text = "If [ $x > 0 ]\nSet Variable [ $y ; Value: 1 ]\nEnd If";
        var ranges = MultiLineStatementRanges.Compute(text);
        Assert.Equal(3, ranges.Count);
        Assert.Equal((1, 1), ranges[0]);
        Assert.Equal((2, 2), ranges[1]);
        Assert.Equal((3, 3), ranges[2]);
    }

    [Fact]
    public void Compute_MultiLineCalc_SpansTheRange()
    {
        var text = "Else If [ Case ( $a > 1; 1;\n          $b > 3; 4 ) ]";
        var ranges = MultiLineStatementRanges.Compute(text);
        Assert.Single(ranges);
        Assert.Equal((1, 2), ranges[0]);
    }

    [Fact]
    public void Compute_BackToBackMultiLineSteps_ProducesTwoRanges()
    {
        var text =
            "If [ Case ( $a > 0;\n           1; 0 ) ]\n" +
            "Set Variable [ $y ; Value: Let ( a = 1 ;\n                                 a + 1 ) ]";
        var ranges = MultiLineStatementRanges.Compute(text);
        Assert.Equal(2, ranges.Count);
        Assert.Equal((1, 2), ranges[0]);
        Assert.Equal((3, 4), ranges[1]);
    }

    [Fact]
    public void Compute_BlankLines_AppearAsSingleLineRanges()
    {
        // Blank lines aren't real steps but Compute returns them; consumers
        // that care (e.g. the step-index margin) filter via BuildStepIndex.
        var text = "If [ $x > 0 ]\n\nEnd If";
        var ranges = MultiLineStatementRanges.Compute(text);
        Assert.Equal(3, ranges.Count);
        Assert.Equal((2, 2), ranges[1]);
    }

    [Fact]
    public void BuildStepIndex_AssignsSequentialNumbersToFirstLines()
    {
        var text = "If [ $x > 0 ]\nSet Variable [ $y ; Value: 1 ]\nEnd If";
        var idx = MultiLineStatementRanges.BuildStepIndex(text);
        Assert.Equal(1, idx[1]);
        Assert.Equal(2, idx[2]);
        Assert.Equal(3, idx[3]);
    }

    [Fact]
    public void BuildStepIndex_MultiLineStep_NumberOnlyOnFirstLine()
    {
        var text =
            "If [ $x > 0 ]\n" +
            "Else If [ Case ( $a > 1; 1;\n          $b > 3; 4 ) ]\n" +
            "End If";
        var idx = MultiLineStatementRanges.BuildStepIndex(text);

        Assert.Equal(1, idx[1]);  // If
        Assert.Equal(2, idx[2]);  // Else If first line
        Assert.False(idx.ContainsKey(3));  // continuation — no number
        Assert.Equal(3, idx[4]);  // End If
    }

    [Fact]
    public void BuildStepIndex_BlankLines_AreNumberedAsEmptyCommentSteps()
    {
        // Blank lines in the display map to empty CommentSteps (FM Pro's
        // convention: a blank line in the script editor is a <Step id="89">
        // with empty Text). They consume step numbers just like any other
        // step.
        var text = "If [ $x > 0 ]\n\n\nEnd If";
        var idx = MultiLineStatementRanges.BuildStepIndex(text);

        Assert.Equal(1, idx[1]);
        Assert.Equal(2, idx[2]);
        Assert.Equal(3, idx[3]);
        Assert.Equal(4, idx[4]);
    }

    [Fact]
    public void FindContinuationColumn_MatchesPositionAfterBracketAndSpace()
    {
        // 'If [ ' — '[' at column 3, content starts at column 5
        Assert.Equal(5, MultiLineStatementRanges.FindContinuationColumn("If [ $x > 0 ]"));

        // With 4-space indent: 'If [ ' starts at col 4, '[' at col 7, content at col 9
        Assert.Equal(9, MultiLineStatementRanges.FindContinuationColumn("    If [ $x > 0 ]"));

        // No bracket
        Assert.Equal(-1, MultiLineStatementRanges.FindContinuationColumn("End If"));
    }

    // --- FindRangeContainingLine ---

    [Fact]
    public void FindRangeContainingLine_SingleLineStep_ReturnsSelf()
    {
        var text = "If [ $x > 0 ]\nEnd If";
        var ranges = MultiLineStatementRanges.Compute(text);

        var hit = MultiLineStatementRanges.FindRangeContainingLine(ranges, 1);
        Assert.Equal((1, 1), hit);

        hit = MultiLineStatementRanges.FindRangeContainingLine(ranges, 2);
        Assert.Equal((2, 2), hit);
    }

    [Fact]
    public void FindRangeContainingLine_MultiLineStep_ReturnsRangeFromAnyLineInIt()
    {
        var text = "Else If [ Case ( $a > 1; 1;\n          $b > 3; 4 ) ]";
        var ranges = MultiLineStatementRanges.Compute(text);

        // Caret on first line of the multi-line step
        Assert.Equal((1, 2), MultiLineStatementRanges.FindRangeContainingLine(ranges, 1));
        // Caret on continuation line
        Assert.Equal((1, 2), MultiLineStatementRanges.FindRangeContainingLine(ranges, 2));
    }

    [Fact]
    public void FindRangeContainingLine_LineOutsideAllRanges_ReturnsNull()
    {
        var text = "If [ $x > 0 ]";
        var ranges = MultiLineStatementRanges.Compute(text);
        Assert.Null(MultiLineStatementRanges.FindRangeContainingLine(ranges, 99));
    }
}
