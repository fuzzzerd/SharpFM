using AvaloniaEdit.Document;
using SharpFM.Scripting.Editor;
using Xunit;

namespace SharpFM.Tests.Scripting.Editor;

/// <summary>
/// Cache-key behavior for the per-document range cache that backs the
/// continuation rail, statement highlight, and step-index margin.
/// Regressions here surface as stale ranges → wrong renders, so the
/// reference-equality and post-edit invalidation guarantees matter.
/// </summary>
public class CachedMultiLineRangesTests
{
    private const string SampleText =
        "If [ $x > 0 ]\n" +
        "Set Variable [ $y ; Value: 1 ]\n" +
        "End If";

    [Fact]
    public void Compute_SameVersion_ReturnsSameInstance()
    {
        // Reference-equality is the contract: callers (StatementHighlight
        // layer, ContinuationRail layer, StepIndexMargin) all hit Compute
        // independently within a single paint pass. They reuse one list
        // only if the cache returns the same instance.
        var doc = new TextDocument(SampleText);

        var first = CachedMultiLineRanges.Compute(doc);
        var second = CachedMultiLineRanges.Compute(doc);

        Assert.Same(first, second);
    }

    [Fact]
    public void Compute_AfterEdit_ReturnsFreshRanges()
    {
        var doc = new TextDocument(SampleText);
        var before = CachedMultiLineRanges.Compute(doc);
        Assert.Equal(3, before.Count);

        doc.Insert(0, "Beep\n");
        var after = CachedMultiLineRanges.Compute(doc);

        Assert.NotSame(before, after);
        Assert.Equal(4, after.Count);
    }

    [Fact]
    public void GetStepIndex_MapsFirstLineToStepNumber()
    {
        var doc = new TextDocument(SampleText);
        var index = CachedMultiLineRanges.GetStepIndex(doc);

        Assert.Equal(1, index[1]); // If
        Assert.Equal(2, index[2]); // Set Variable
        Assert.Equal(3, index[3]); // End If
    }

    [Fact]
    public void GetStepIndex_SameVersion_ReturnsSameInstance()
    {
        var doc = new TextDocument(SampleText);

        var first = CachedMultiLineRanges.GetStepIndex(doc);
        var second = CachedMultiLineRanges.GetStepIndex(doc);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetStepIndex_AfterEdit_RecomputesAlongsideRanges()
    {
        // Step-index shares the cache version with Compute. If only the
        // ranges entry was bumped on edit, the step-index would go stale
        // and StepIndexMargin would render wrong numbers.
        var doc = new TextDocument(SampleText);
        var before = CachedMultiLineRanges.GetStepIndex(doc);
        Assert.Equal(3, before.Count);

        doc.Insert(0, "Beep\n");
        var after = CachedMultiLineRanges.GetStepIndex(doc);

        Assert.NotSame(before, after);
        Assert.Equal(4, after.Count);
        Assert.Equal(1, after[1]); // newly-inserted Beep
    }
}
