using System;
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

    [Fact]
    public void GetCall_EmptyArg_SuggestsAllSelectorKeywords()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("Get(", 4);
        Assert.Equal(CalcCompletionContext.FunctionParam, ctx);
        Assert.Contains(items, i => i.Text == "AccountName");
        Assert.Contains(items, i => i.Text == "CurrentDate");
        Assert.Contains(items, i => i.Text == "SystemPlatform");
    }

    [Fact]
    public void GetCall_PartialPrefix_FiltersKeywords()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("Get(Acc", 7);
        Assert.Equal(CalcCompletionContext.FunctionParam, ctx);
        var names = items.Select(i => i.Text).ToList();
        Assert.Contains("AccountName", names);
        Assert.Contains("AccountPrivilegeSetName", names);
        Assert.DoesNotContain("CurrentDate", names);
    }

    [Fact]
    public void GetCall_KeywordTooltipCarriesDescription()
    {
        var (_, items) = FmCalcCompletionProvider.GetCompletions("Get(AccountName", 15);
        var item = items.First(i => i.Text == "AccountName");
        Assert.Contains("account", item.Description.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonSetElement_LastArg_SuggestsJsonTypes()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("JSONSetElement(j;\"k\";v;", 23);
        Assert.Equal(CalcCompletionContext.FunctionParam, ctx);
        Assert.Contains(items, i => i.Text == "JSONString");
        Assert.Contains(items, i => i.Text == "JSONNumber");
    }

    [Fact]
    public void JsonSetElement_EarlierArg_DoesNotSuggestJsonTypes()
    {
        // Caret is on arg 0 (json) — no enum values defined for that param.
        var (ctx, _) = FmCalcCompletionProvider.GetCompletions("JSONSetElement(", 15);
        Assert.NotEqual(CalcCompletionContext.FunctionParam, ctx);
    }

    [Fact]
    public void TextStyleAdd_SecondArg_SuggestsStyleKeywords()
    {
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("TextStyleAdd(text;", 18);
        Assert.Equal(CalcCompletionContext.FunctionParam, ctx);
        Assert.Contains(items, i => i.Text == "Bold");
        Assert.Contains(items, i => i.Text == "Italic");
    }

    [Fact]
    public void OpenParenInsideString_DoesNotTriggerFunctionParamContext()
    {
        var (ctx, _) = FmCalcCompletionProvider.GetCompletions("\"Get(\" & A", 10);
        Assert.NotEqual(CalcCompletionContext.FunctionParam, ctx);
    }

    [Fact]
    public void NestedCall_SuggestsForInnermostCall()
    {
        // Outer is JSONSetElement, inner is Get — caret is inside Get, so
        // we should see Get's selector keywords, not JSON types.
        var (ctx, items) = FmCalcCompletionProvider.GetCompletions("JSONSetElement(j;k;Get(Acc", 26);
        Assert.Equal(CalcCompletionContext.FunctionParam, ctx);
        Assert.Contains(items, i => i.Text == "AccountName");
        Assert.DoesNotContain(items, i => i.Text == "JSONString");
    }

    [Fact]
    public void DetectEnclosingCall_OutsideAnyCall_ReturnsNull()
    {
        Assert.Null(FmCalcCompletionProvider.DetectEnclosingCall("Length(x) + 1", 13));
    }

    [Fact]
    public void DetectEnclosingCall_AfterClosingParen_ReturnsNull()
    {
        Assert.Null(FmCalcCompletionProvider.DetectEnclosingCall("Get(x)", 6));
    }

    [Fact]
    public void DetectEnclosingCall_CountsArgsCorrectly()
    {
        var r = FmCalcCompletionProvider.DetectEnclosingCall("F(a;b;c", 7);
        Assert.NotNull(r);
        Assert.Equal("F", r.Value.FunctionName);
        Assert.Equal(2, r.Value.ArgIndex);
    }

    [Fact]
    public void DetectEnclosingCall_IgnoresSemicolonsInNestedCalls()
    {
        var r = FmCalcCompletionProvider.DetectEnclosingCall("F(G(a;b);c", 10);
        Assert.NotNull(r);
        Assert.Equal("F", r.Value.FunctionName);
        Assert.Equal(1, r.Value.ArgIndex);
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
