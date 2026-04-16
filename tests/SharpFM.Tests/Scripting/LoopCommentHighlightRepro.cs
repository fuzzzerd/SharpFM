using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Repro for the reported bug: a Loop with several comment lines and a
/// Set Variable calc apparently gets highlighted as one range instead of
/// per-step. If <see cref="MultiLineStatementRanges.Compute"/> returns
/// a single multi-line range for this fixture, the bug reproduces.
/// </summary>
public class LoopCommentHighlightRepro
{
    private const string ProblematicScript =
        "Loop\n" +
        "    # BEGIN: Error-handling Loop:\n" +
        "    # ------------------------------------\n" +
        "    # \n" +
        "    # \n" +
        "    # ----------------------------------------\n" +
        "    # Init variables\n" +
        "    # \n" +
        "    Set Variable [ $didSetBreakPointFieldValue ; Value: DictContains ( $params ; \"BreakPointFieldValue\" ) ]";

    [Fact]
    public void Compute_EachLineIsItsOwnRange()
    {
        var ranges = MultiLineStatementRanges.Compute(ProblematicScript);

        // Expect 9 ranges, one per line, each single-line (start == end).
        Assert.Equal(9, ranges.Count);
        Assert.All(ranges, r => Assert.Equal(r.StartLine, r.EndLine));
    }
}
