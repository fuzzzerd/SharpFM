using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;
using SharpFM.Model.Scripting.Calc;

namespace SharpFM.Scripting.Editor;

public enum CalcCompletionContext
{
    None,
    Identifier,
    Variable,
    FieldRef,
    String,
    Comment,
}

/// <summary>
/// Completion provider for the FileMaker calculation editor. Reads exclusively
/// from <see cref="FmCalcCatalog"/> — same source of truth that drives the
/// TextMate grammar, so highlighting and completions can never disagree.
///
/// Completion items carry a signature + one-line description in their
/// tooltip; control forms (<c>Let</c>, <c>Case</c>, …) expand from snippet
/// templates with the first parameter pre-selected for editing.
///
/// <para>Phase-3 hooks: <see cref="GetCompletions(string, int, ICalcCompletionContextProvider?)"/>
/// accepts an optional context provider that supplies <c>$variables</c>
/// in scope and field references for the current calc. The window passes a
/// real one; tests pass <c>null</c> and only exercise built-in completions.</para>
/// </summary>
public static class FmCalcCompletionProvider
{
    public static (CalcCompletionContext Context, IList<ICompletionData> Items) GetCompletions(
        string lineText, int caretColumn, ICalcCompletionContextProvider? contextProvider = null)
    {
        if (caretColumn < 0) caretColumn = 0;
        if (caretColumn > lineText.Length) caretColumn = lineText.Length;

        var beforeCaret = lineText.Substring(0, caretColumn);

        if (IsInsideString(beforeCaret))
            return (CalcCompletionContext.String, Array.Empty<ICompletionData>());

        if (IsInsideLineComment(beforeCaret))
            return (CalcCompletionContext.Comment, Array.Empty<ICompletionData>());

        // Walk back from caret over identifier characters to find the prefix
        // we're currently typing. Then look at what comes immediately before
        // it to decide whether we're naming a function (default), a $variable,
        // or the field side of a Table::Field reference.
        var prefixStart = caretColumn;
        while (prefixStart > 0 && IsIdentifierChar(lineText[prefixStart - 1]))
            prefixStart--;
        var prefix = lineText.Substring(prefixStart, caretColumn - prefixStart);

        // $variable or $$global
        if (prefixStart >= 1 && lineText[prefixStart - 1] == '$')
        {
            var dollars = (prefixStart >= 2 && lineText[prefixStart - 2] == '$') ? "$$" : "$";
            var vars = contextProvider?.GetVariablesInScope(lineText, prefixStart - dollars.Length) ?? Array.Empty<string>();
            var items = vars
                .Where(v => v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(v => (ICompletionData)new FmScriptCompletionData(v, "variable"))
                .ToList();
            return (CalcCompletionContext.Variable, items);
        }

        // Field side of Table::Field — caret is right after `::`
        if (prefixStart >= 2 && lineText[prefixStart - 1] == ':' && lineText[prefixStart - 2] == ':')
        {
            var tableNameStart = prefixStart - 2;
            while (tableNameStart > 0 && IsIdentifierChar(lineText[tableNameStart - 1]))
                tableNameStart--;
            var tableName = lineText.Substring(tableNameStart, (prefixStart - 2) - tableNameStart);
            var fields = contextProvider?.GetFieldsForTable(tableName) ?? Array.Empty<string>();
            var items = fields
                .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(f => (ICompletionData)new FmScriptCompletionData(f, $"{tableName}::{f}"))
                .ToList();
            return (CalcCompletionContext.FieldRef, items);
        }

        // Default: identifier — functions, control forms, constants.
        // (Plus tables for the head of a Table::Field, when phase-3 context
        // is wired up.)
        var identifierItems = BuildIdentifierCompletions(prefix, contextProvider);
        return (CalcCompletionContext.Identifier, identifierItems);
    }

    /// <summary>
    /// All built-in identifier completions (functions, control forms,
    /// constants), built once at startup. The catalog is immutable after
    /// type initialization, so this list is shared safely across every
    /// calculation editor — no per-trigger allocations beyond the filtered
    /// view we hand to the completion window.
    ///
    /// <para>Public so future consumers (e.g. embedded calc inside script
    /// step brackets) can reuse the same list.</para>
    /// </summary>
    public static IReadOnlyList<ICompletionData> AllBuiltinIdentifierCompletions { get; } =
        BuildAllBuiltinIdentifierCompletions();

    private static IReadOnlyList<ICompletionData> BuildAllBuiltinIdentifierCompletions()
    {
        var items = new List<ICompletionData>(
            FmCalcCatalog.ControlForms.Count
            + FmCalcCatalog.Functions.Count
            + FmCalcCatalog.Constants.Count);

        // Control forms first — promoted via priority so Let/Case/If sort
        // ahead of similarly-named functions when prefixes overlap.
        foreach (var c in FmCalcCatalog.ControlForms)
        {
            items.Add(new FmScriptCompletionData(
                c.Name,
                $"{c.Signature} — {c.Description}",
                priority: 1.0,
                snippet: c.Snippet));
        }

        foreach (var f in FmCalcCatalog.Functions)
        {
            items.Add(new FmScriptCompletionData(
                f.Name,
                $"{f.Signature} — {f.Description}"));
        }

        foreach (var k in FmCalcCatalog.Constants)
        {
            items.Add(new FmScriptCompletionData(k, "constant"));
        }

        return items;
    }

    private static IList<ICompletionData> BuildIdentifierCompletions(
        string prefix, ICalcCompletionContextProvider? contextProvider)
    {
        // Filter the prebuilt list — no per-trigger allocations of completion
        // data objects, just a List of references.
        var items = new List<ICompletionData>();
        foreach (var item in AllBuiltinIdentifierCompletions)
        {
            if (Matches(item.Text, prefix)) items.Add(item);
        }

        // Tables come from the document context, so they're built per-trigger.
        // Empty in practice today (no schema container); the loop is a no-op
        // until that hook is wired.
        if (contextProvider != null)
        {
            foreach (var t in contextProvider.GetTableNames())
            {
                if (!Matches(t, prefix)) continue;
                items.Add(new FmScriptCompletionData(t, "table"));
            }
        }

        return items;
    }

    private static bool Matches(string candidate, string prefix) =>
        prefix.Length == 0 || candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// True if the caret sits inside an unterminated double-quoted string
    /// on this line. Counts unescaped quote characters — odd → inside.
    /// </summary>
    private static bool IsInsideString(string textBeforeCaret)
    {
        bool inside = false;
        for (int i = 0; i < textBeforeCaret.Length; i++)
        {
            var ch = textBeforeCaret[i];
            if (ch == '\\' && i + 1 < textBeforeCaret.Length)
            {
                i++;
                continue;
            }
            if (ch == '"') inside = !inside;
        }
        return inside;
    }

    /// <summary>
    /// True if the caret sits inside a <c>//</c> line comment that begins
    /// before the caret on this line and is not itself inside a string.
    /// </summary>
    private static bool IsInsideLineComment(string textBeforeCaret)
    {
        bool insideString = false;
        for (int i = 0; i < textBeforeCaret.Length - 1; i++)
        {
            var ch = textBeforeCaret[i];
            if (ch == '\\' && i + 1 < textBeforeCaret.Length)
            {
                i++;
                continue;
            }
            if (ch == '"') insideString = !insideString;
            else if (!insideString && ch == '/' && textBeforeCaret[i + 1] == '/')
                return true;
        }
        return false;
    }
}

/// <summary>
/// Phase-3 extension point: lets the calculation editor supply
/// document-derived completions (<c>$Let</c> bindings in scope, table /
/// field lists from the FmField context). Implementations can return empty
/// lists to opt out.
/// </summary>
public interface ICalcCompletionContextProvider
{
    /// <summary>
    /// Local and global variables visible at the given caret offset on the
    /// supplied line. Names should be the bare identifier — no <c>$</c> or
    /// <c>$$</c> prefix; the provider already knows from context.
    /// </summary>
    IReadOnlyList<string> GetVariablesInScope(string lineText, int offset);

    /// <summary>Names of tables (or table occurrences) usable as the LHS of <c>Table::Field</c>.</summary>
    IReadOnlyList<string> GetTableNames();

    /// <summary>Field names for the named table.</summary>
    IReadOnlyList<string> GetFieldsForTable(string tableName);
}
