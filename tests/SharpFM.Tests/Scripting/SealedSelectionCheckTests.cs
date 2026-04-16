using SharpFM.Editors.SealedSteps;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Predicate that decides whether a text selection crosses into a sealed
/// step's line range. Used by the editor to reject copy/cut operations
/// that would span a sealed line (which has no display-text
/// representation the clipboard can faithfully carry).
/// </summary>
public class SealedSelectionCheckTests
{
    [Fact]
    public void SelectionEntirelyInsideSingleSealedLine_Spans_True()
    {
        // selection 10..20 is inside sealed line 5..100
        Assert.True(SealedSelectionCheck.SpansSealed(10, 20, new[] { (5, 100) }));
    }

    [Fact]
    public void SelectionEntirelyBeforeSealed_Spans_False()
    {
        Assert.False(SealedSelectionCheck.SpansSealed(0, 4, new[] { (5, 100) }));
    }

    [Fact]
    public void SelectionEntirelyAfterSealed_Spans_False()
    {
        Assert.False(SealedSelectionCheck.SpansSealed(101, 200, new[] { (5, 100) }));
    }

    [Fact]
    public void SelectionCrossesIntoSealedFromAbove_Spans_True()
    {
        // Selection 0..10 touches sealed line 5..100
        Assert.True(SealedSelectionCheck.SpansSealed(0, 10, new[] { (5, 100) }));
    }

    [Fact]
    public void SelectionCrossesOutOfSealed_Spans_True()
    {
        // Selection 50..150 exits sealed line at 100
        Assert.True(SealedSelectionCheck.SpansSealed(50, 150, new[] { (5, 100) }));
    }

    [Fact]
    public void EmptySelection_Spans_False()
    {
        Assert.False(SealedSelectionCheck.SpansSealed(50, 50, new[] { (5, 100) }));
    }

    [Fact]
    public void MultipleSealedRanges_AnyCrossing_SpansTrue()
    {
        Assert.True(SealedSelectionCheck.SpansSealed(
            120, 130,
            new[] { (5, 100), (115, 140) }));
    }

    [Fact]
    public void NoSealedRanges_Spans_False()
    {
        Assert.False(SealedSelectionCheck.SpansSealed(0, 1000, System.Array.Empty<(int, int)>()));
    }
}
