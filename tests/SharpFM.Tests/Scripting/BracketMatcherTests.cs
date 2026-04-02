using SharpFM.Scripting.Parsing;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class BracketMatcherTests
{
    // --- FindTopLevelOpenBracket ---

    [Fact]
    public void FindTopLevelOpenBracket_SimpleBracket_ReturnsIndex()
    {
        Assert.Equal(4, BracketMatcher.FindTopLevelOpenBracket("test[value]"));
    }

    [Fact]
    public void FindTopLevelOpenBracket_NoBracket_ReturnsNegativeOne()
    {
        Assert.Equal(-1, BracketMatcher.FindTopLevelOpenBracket("no brackets here"));
    }

    [Fact]
    public void FindTopLevelOpenBracket_BracketInsideQuotes_Ignored()
    {
        Assert.Equal(-1, BracketMatcher.FindTopLevelOpenBracket("\"quoted[text]\""));
    }

    [Fact]
    public void FindTopLevelOpenBracket_BracketInsideParens_Ignored()
    {
        Assert.Equal(-1, BracketMatcher.FindTopLevelOpenBracket("(nested[bracket])"));
    }

    [Fact]
    public void FindTopLevelOpenBracket_BracketAfterParens_Found()
    {
        Assert.Equal(3, BracketMatcher.FindTopLevelOpenBracket("(x)[y]"));
    }

    [Fact]
    public void FindTopLevelOpenBracket_EscapedQuoteDoesNotCloseString()
    {
        // "abc\" is an escaped quote inside the string, so the [ is still quoted
        Assert.Equal(-1, BracketMatcher.FindTopLevelOpenBracket("\"abc\\\"[x]\""));
    }

    // --- FindMatchingClose ---

    [Fact]
    public void FindMatchingClose_SimpleCase_ReturnsClosingIndex()
    {
        Assert.Equal(6, BracketMatcher.FindMatchingClose("test[x]end", 4));
    }

    [Fact]
    public void FindMatchingClose_NestedBrackets_FindsOuterClose()
    {
        Assert.Equal(7, BracketMatcher.FindMatchingClose("a[b[c]d]e", 1));
    }

    [Fact]
    public void FindMatchingClose_Unmatched_ReturnsNegativeOne()
    {
        Assert.Equal(-1, BracketMatcher.FindMatchingClose("a[open", 1));
    }

    [Fact]
    public void FindMatchingClose_BracketInQuotes_IgnoredInDepth()
    {
        // a[\"]\"bc]d — the ] inside quotes is ignored, outer ] is at index 7
        Assert.Equal(7, BracketMatcher.FindMatchingClose("a[\"]\"bc]d", 1));
    }

    // --- FindMatchingOpen ---

    [Fact]
    public void FindMatchingOpen_SimpleCase_ReturnsOpenIndex()
    {
        // For FindMatchingOpen, closePos should point to the char *before* the ']' we're matching
        // The method scans backward from closePos
        var text = "test[x]end";
        Assert.Equal(4, BracketMatcher.FindMatchingOpen(text, 5));
    }

    [Fact]
    public void FindMatchingOpen_NestedBrackets_FindsOuterOpen()
    {
        Assert.Equal(1, BracketMatcher.FindMatchingOpen("a[b[c]d]e", 6));
    }

    [Fact]
    public void FindMatchingOpen_Unmatched_ReturnsNegativeOne()
    {
        Assert.Equal(-1, BracketMatcher.FindMatchingOpen("close]", 4));
    }

    // --- HasUnbalancedBrackets ---

    [Fact]
    public void HasUnbalancedBrackets_Balanced_ReturnsFalse()
    {
        Assert.False(BracketMatcher.HasUnbalancedBrackets("[a] [b]"));
    }

    [Fact]
    public void HasUnbalancedBrackets_MoreOpens_ReturnsTrue()
    {
        Assert.True(BracketMatcher.HasUnbalancedBrackets("[a [b]"));
    }

    [Fact]
    public void HasUnbalancedBrackets_MoreCloses_ReturnsFalse()
    {
        // depth goes negative but ends negative, not > 0
        Assert.False(BracketMatcher.HasUnbalancedBrackets("a] [b"));
    }

    [Fact]
    public void HasUnbalancedBrackets_BracketsInsideQuotes_Ignored()
    {
        Assert.False(BracketMatcher.HasUnbalancedBrackets("\"[unmatched\""));
    }

    // --- CountBracketDepth ---

    [Fact]
    public void CountBracketDepth_Balanced_ReturnsZero()
    {
        Assert.Equal(0, BracketMatcher.CountBracketDepth("[x]"));
    }

    [Fact]
    public void CountBracketDepth_OneOpen_ReturnsOne()
    {
        Assert.Equal(1, BracketMatcher.CountBracketDepth("[x"));
    }

    [Fact]
    public void CountBracketDepth_OneClose_ReturnsNegativeOne()
    {
        Assert.Equal(-1, BracketMatcher.CountBracketDepth("x]"));
    }

    [Fact]
    public void CountBracketDepth_QuotedBrackets_Ignored()
    {
        Assert.Equal(0, BracketMatcher.CountBracketDepth("\"[\""));
    }

    // --- SplitParams ---

    [Fact]
    public void SplitParams_SimpleSemicolon_Splits()
    {
        var result = BracketMatcher.SplitParams("a ; b ; c");
        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void SplitParams_SemicolonInQuotes_NotSplit()
    {
        var result = BracketMatcher.SplitParams("\"a;b\" ; c");
        Assert.Equal(["\"a;b\"", "c"], result);
    }

    [Fact]
    public void SplitParams_SemicolonInParens_NotSplit()
    {
        var result = BracketMatcher.SplitParams("func(a;b) ; c");
        Assert.Equal(["func(a;b)", "c"], result);
    }

    [Fact]
    public void SplitParams_SemicolonInBrackets_NotSplit()
    {
        var result = BracketMatcher.SplitParams("[a;b] ; c");
        Assert.Equal(["[a;b]", "c"], result);
    }

    [Fact]
    public void SplitParams_EmptyString_ReturnsEmpty()
    {
        var result = BracketMatcher.SplitParams("");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitParams_NoSemicolon_ReturnsSingle()
    {
        var result = BracketMatcher.SplitParams("just one param");
        Assert.Equal(["just one param"], result);
    }

    [Fact]
    public void SplitParams_EscapedQuote_HandledCorrectly()
    {
        var result = BracketMatcher.SplitParams("\"a\\\"b\" ; c");
        Assert.Equal(["\"a\\\"b\"", "c"], result);
    }
}
