using AvaloniaEdit.Document;
using SharpFM.Scripting.Editor;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Auto-indent on Enter when the caret is inside a multi-line script step.
/// New lines indent to the column just after the owning step's opening '['.
/// Outside multi-line regions the strategy is a no-op.
/// </summary>
public class ContinuationIndentStrategyTests
{
    [Fact]
    public void Enter_inside_unbalanced_bracket_indents_to_bracket_column()
    {
        // After Enter is pressed inside `Set Variable [ $x ; Value:`,
        // line 2 should land at the column just after the '[ '.
        var doc = new TextDocument("Set Variable [ $x ; Value:\n");
        var strategy = new ContinuationIndentStrategy();
        var line2 = doc.GetLineByNumber(2);

        strategy.IndentLine(doc, line2);

        // 'Set Variable [ ' — '[' at col 13 (0-indexed), content at col 15
        var expected = new string(' ', 15);
        var line2Text = doc.GetText(line2.Offset, line2.Length);
        Assert.Equal(expected, line2Text);
    }

    [Fact]
    public void Enter_outside_brackets_no_indent_added()
    {
        var doc = new TextDocument("End If\n");
        var strategy = new ContinuationIndentStrategy();
        var line2 = doc.GetLineByNumber(2);

        strategy.IndentLine(doc, line2);

        Assert.Equal(string.Empty, doc.GetText(line2.Offset, line2.Length));
    }

    [Fact]
    public void Enter_inside_nested_block_uses_owning_step_column()
    {
        // Outer If opens, inner Set Variable's calc spans multiple lines.
        // The new line inside Set Variable's calc should indent to the
        // Set Variable's bracket column (deep), not the outer If's.
        var doc = new TextDocument(
            "If [ $x > 0 ]\n" +
            "    Set Variable [ $y ; Value:\n");

        var strategy = new ContinuationIndentStrategy();
        var line3 = doc.GetLineByNumber(2); // inside Set Variable

        // Wait — line 2 IS the Set Variable line. We need to add line 3.
        // Insert a newline after line 2 and indent that new line.
        doc.Insert(doc.TextLength, "\n");
        var newLine = doc.GetLineByNumber(3);

        strategy.IndentLine(doc, newLine);

        // '    Set Variable [ ' = 4 + 13 + 2 = column 19 for content
        var expected = new string(' ', 19);
        Assert.Equal(expected, doc.GetText(newLine.Offset, newLine.Length));
    }

    [Fact]
    public void IndentLines_is_noop_for_paste()
    {
        // Pasting a block must not mangle leading whitespace.
        var original = "If [ $x > 0 ]\n    Set Variable [ $y ; Value:\n        a + 1 ) ]";
        var doc = new TextDocument(original);
        var strategy = new ContinuationIndentStrategy();

        strategy.IndentLines(doc, 1, 3);

        Assert.Equal(original, doc.Text);
    }
}
