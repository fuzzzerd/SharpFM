using AvaloniaEdit.Document;
using AvaloniaEdit.Indentation;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Indentation strategy that auto-indents new lines created with Enter
/// inside an unbalanced-bracket region (i.e. inside a multi-line script
/// step's calculation). The new line is indented to match the column
/// just after the owning step's opening <c>[ </c>, so the user's caret
/// lands aligned with the calc content above. Outside of unbalanced
/// regions the strategy is a no-op (no extra indent applied).
/// </summary>
public class ContinuationIndentStrategy : IIndentationStrategy
{
    public void IndentLine(TextDocument document, DocumentLine line)
    {
        if (line.LineNumber <= 1) return;

        // Find the owning multi-line statement (if any) by scanning the
        // ranges helper and checking if our new line falls inside one.
        var text = document.Text;
        var ranges = MultiLineStatementRanges.Compute(text);

        foreach (var (startLine, endLine) in ranges)
        {
            if (startLine == endLine) continue;
            if (line.LineNumber <= startLine) continue;
            if (line.LineNumber > endLine) continue;

            // Inside this multi-line statement. Compute target indent from
            // the first line's bracket column.
            var firstLine = document.GetLineByNumber(startLine);
            var firstText = document.GetText(firstLine.Offset, firstLine.Length);
            var col = MultiLineStatementRanges.FindContinuationColumn(firstText);
            if (col < 0) return;

            // Replace the line's existing leading whitespace (often none,
            // since AvaloniaEdit just inserted a bare newline) with the
            // target indent.
            var existing = document.GetText(line.Offset, line.Length);
            var existingLeadingSpaces = 0;
            while (existingLeadingSpaces < existing.Length && existing[existingLeadingSpaces] == ' ')
                existingLeadingSpaces++;

            if (existingLeadingSpaces == col) return; // already correct

            document.Replace(line.Offset, existingLeadingSpaces, new string(' ', col));
            return;
        }
    }

    public void IndentLines(TextDocument document, int beginLine, int endLine)
    {
        // No-op for paste / multi-line block inserts. Preserve the user's
        // pasted indentation exactly; auto-indent only fires on Enter.
    }
}
