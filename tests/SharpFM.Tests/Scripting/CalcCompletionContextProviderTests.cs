using SharpFM.Model.Schema;
using SharpFM.Scripting.Editor;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class CalcCompletionContextProviderTests
{
    [Fact]
    public void GetVariablesInScope_ReturnsAllUniqueDollarTokens()
    {
        var doc = "Let([$x = 1; $y = $x + 2]; $$global + $x)";
        var provider = new CalcCompletionContextProvider(() => doc, currentTable: null);
        var vars = provider.GetVariablesInScope("", 0);
        Assert.Contains("x", vars);
        Assert.Contains("y", vars);
        Assert.Contains("global", vars);
        // No duplicates — $x appears three times in source
        Assert.Equal(3, vars.Count);
    }

    [Fact]
    public void GetVariablesInScope_EmptyDocument_ReturnsEmpty()
    {
        var provider = new CalcCompletionContextProvider(() => "", currentTable: null);
        Assert.Empty(provider.GetVariablesInScope("", 0));
    }

    [Fact]
    public void GetFieldsForTable_MatchingTableName_ReturnsFieldNames()
    {
        var table = new FmTable("Customer");
        table.AddField(new FmField { Name = "Name", Id = 1 });
        table.AddField(new FmField { Name = "Email", Id = 2 });

        var provider = new CalcCompletionContextProvider(() => "", currentTable: table);
        var fields = provider.GetFieldsForTable("Customer");
        Assert.Equal(new[] { "Name", "Email" }, fields);
    }

    [Fact]
    public void GetFieldsForTable_DifferentTableName_ReturnsEmpty()
    {
        var table = new FmTable("Customer");
        table.AddField(new FmField { Name = "Name", Id = 1 });

        var provider = new CalcCompletionContextProvider(() => "", currentTable: table);
        Assert.Empty(provider.GetFieldsForTable("Invoice"));
    }

    [Fact]
    public void GetFieldsForTable_NoTable_ReturnsEmpty()
    {
        var provider = new CalcCompletionContextProvider(() => "", currentTable: null);
        Assert.Empty(provider.GetFieldsForTable("Anything"));
    }

    [Fact]
    public void GetTableNames_ReturnsEmpty()
    {
        // Until a schema container exists, table enumeration is unavailable
        // — the provider returns empty rather than guess.
        var table = new FmTable("Customer");
        var provider = new CalcCompletionContextProvider(() => "", currentTable: table);
        Assert.Empty(provider.GetTableNames());
    }
}
