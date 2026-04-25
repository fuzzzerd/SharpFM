using System.Collections.Generic;
using System.Linq;
using SharpFM.Scripting.Editor;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class FmCalcCompletionProviderTests
{
    [Fact]
    public void EmptyInput_ReturnsAllIdentifierCompletions()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("", 0);
        Assert.Equal(CalcCompletionContext.Identifier, ctx);
        Assert.Contains(items, i => i.Text == "Length");
        Assert.Contains(items, i => i.Text == "Let");
        Assert.Contains(items, i => i.Text == "True");
    }

    [Fact]
    public void Prefix_FiltersItemsCaseInsensitively()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("Le", 2);
        Assert.Equal(CalcCompletionContext.Identifier, ctx);
        var names = items.Select(i => i.Text).ToList();
        Assert.Contains("Length", names);
        Assert.Contains("Left", names);
        Assert.Contains("Let", names);
        Assert.DoesNotContain("Sum", names);
    }

    [Fact]
    public void Prefix_LowerCase_StillMatchesCapitalizedFunctions()
    {
        var (_, items) = FmCalcCompletionProvider.GetCompletions("len", 3);
        Assert.Contains(items, i => i.Text == "Length");
    }

    [Fact]
    public void ControlForm_DescriptionIncludesSignature()
    {
        var (_, items) = FmCalcCompletionProvider.GetCompletions("Let", 3);
        var let = items.First(i => i.Text == "Let");
        var desc = let.Description.ToString()!;
        Assert.Contains("Let([var = expr", desc);
    }

    [Fact]
    public void Function_DescriptionIncludesSignature()
    {
        var (_, items) = FmCalcCompletionProvider.GetCompletions("Length", 6);
        var fn = items.First(i => i.Text == "Length");
        Assert.Contains("Length(text)", fn.Description.ToString()!);
    }

    [Fact]
    public void InsideString_ReturnsNoCompletions()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("\"Le", 3);
        Assert.Equal(CalcCompletionContext.String, ctx);
        Assert.Empty(items);
    }

    [Fact]
    public void AfterClosingQuote_ReturnsCompletionsAgain()
    {
        var (ctx, _) = FmCalcCompletionProvider.GetCompletions("\"text\" & Le", 11);
        Assert.Equal(CalcCompletionContext.Identifier, ctx);
    }

    [Fact]
    public void EscapedQuoteInString_StillTreatedAsInside()
    {
        var (ctx, _) = FmCalcCompletionProvider.GetCompletions("\"a\\\"b Le", 8);
        Assert.Equal(CalcCompletionContext.String, ctx);
    }

    [Fact]
    public void InsideLineComment_ReturnsNoCompletions()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("// Le", 5);
        Assert.Equal(CalcCompletionContext.Comment, ctx);
        Assert.Empty(items);
    }

    [Fact]
    public void Variable_AfterDollarSign_ReturnsVariableContext()
    {
        var provider = new StubContext(variables: new[] { "myLocal", "counter" });
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("$my", 3, provider);
        Assert.Equal(CalcCompletionContext.Variable, ctx);
        Assert.Contains(items, i => i.Text == "myLocal");
        Assert.DoesNotContain(items, i => i.Text == "counter");
    }

    [Fact]
    public void Variable_AfterDoubleDollarSign_ReturnsVariableContext()
    {
        var provider = new StubContext(variables: new[] { "globalA", "globalB" });
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("$$g", 3, provider);
        Assert.Equal(CalcCompletionContext.Variable, ctx);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Variable_NoContextProvider_ReturnsEmptyVariableList()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("$my", 3);
        Assert.Equal(CalcCompletionContext.Variable, ctx);
        Assert.Empty(items);
    }

    [Fact]
    public void FieldRef_AfterDoubleColon_SuggestsFieldsForTable()
    {
        var provider = new StubContext(fields: new() { ["Customer"] = new[] { "Name", "Email" } });
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("Customer::N", 11, provider);
        Assert.Equal(CalcCompletionContext.FieldRef, ctx);
        Assert.Contains(items, i => i.Text == "Name");
        Assert.DoesNotContain(items, i => i.Text == "Email");
    }

    [Fact]
    public void TableNames_FromContext_AppearInIdentifierCompletions()
    {
        var provider = new StubContext(tables: new[] { "Customer", "Invoice" });
        var (_, items) = FmCalcCompletionProvider.GetCompletions("Cus", 3, provider);
        Assert.Contains(items, i => i.Text == "Customer");
    }

    private sealed class StubContext : ICalcCompletionContextProvider
    {
        private readonly IReadOnlyList<string> _variables;
        private readonly IReadOnlyList<string> _tables;
        private readonly Dictionary<string, IReadOnlyList<string>> _fields;

        public StubContext(
            IReadOnlyList<string>? variables = null,
            IReadOnlyList<string>? tables = null,
            Dictionary<string, string[]>? fields = null)
        {
            _variables = variables ?? System.Array.Empty<string>();
            _tables = tables ?? System.Array.Empty<string>();
            _fields = fields?.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<string>)kv.Value)
                ?? new Dictionary<string, IReadOnlyList<string>>();
        }

        public IReadOnlyList<string> GetVariablesInScope(string lineText, int offset) => _variables;
        public IReadOnlyList<string> GetTableNames() => _tables;
        public IReadOnlyList<string> GetFieldsForTable(string tableName) =>
            _fields.TryGetValue(tableName, out var fs) ? fs : System.Array.Empty<string>();
    }
}
