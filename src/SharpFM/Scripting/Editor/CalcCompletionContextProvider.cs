using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpFM.Model.Schema;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Document-aware completion context for the calculation editor:
///
/// <list type="bullet">
///   <item><description><b>Variables</b> — scrapes <c>$ident</c> and <c>$$ident</c>
///   tokens from the current calculation text. Doesn't attempt proper
///   <c>Let</c>-scope analysis; calcs are short enough that bag-of-names
///   from the document covers the common case (reminding the user of names
///   they've already introduced).</description></item>
///   <item><description><b>Fields</b> — names from the table the field
///   being edited belongs to. Cross-table refs would need the surrounding
///   schema, which the calc editor isn't given today.</description></item>
///   <item><description><b>Tables</b> — empty until a schema container
///   exists that can enumerate table occurrences.</description></item>
/// </list>
/// </summary>
internal sealed class CalcCompletionContextProvider : ICalcCompletionContextProvider
{
    private static readonly Regex VariableRegex =
        new(@"\$\$?([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Compiled);

    private readonly Func<string> _getDocumentText;
    private readonly FmTable? _currentTable;

    public CalcCompletionContextProvider(Func<string> getDocumentText, FmTable? currentTable)
    {
        _getDocumentText = getDocumentText;
        _currentTable = currentTable;
    }

    public IReadOnlyList<string> GetVariablesInScope(string lineText, int offset)
    {
        var doc = _getDocumentText();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (Match m in VariableRegex.Matches(doc))
        {
            var name = m.Groups[1].Value;
            if (seen.Add(name)) result.Add(name);
        }
        return result;
    }

    public IReadOnlyList<string> GetTableNames() => Array.Empty<string>();

    public IReadOnlyList<string> GetFieldsForTable(string tableName)
    {
        if (_currentTable == null) return Array.Empty<string>();
        if (!string.Equals(tableName, _currentTable.Name, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();
        return _currentTable.Fields.Select(f => f.Name).ToList();
    }
}
