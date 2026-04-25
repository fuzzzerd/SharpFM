using System.Linq;
using SharpFM.Model.Scripting.Calc;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

/// <summary>
/// Sanity checks on the calculation catalog and the grammar built from it.
/// The catalog is the single source of truth for the grammar and (next) the
/// completion provider, so duplicates or empty groups would silently
/// degrade both consumers.
/// </summary>
public class FmCalcCatalogTests
{
    [Fact]
    public void Catalog_HasNoDuplicateFunctionNames()
    {
        var dupes = FmCalcCatalog.Functions
            .GroupBy(f => f.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.Empty(dupes);
    }

    [Fact]
    public void Catalog_EveryFunctionHasSignatureAndDescription()
    {
        Assert.All(FmCalcCatalog.Functions, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Signature), $"{f.Name} signature");
            Assert.False(string.IsNullOrWhiteSpace(f.Description), $"{f.Name} description");
        });
    }

    [Fact]
    public void Catalog_ControlFormSnippetsContainTabStops()
    {
        Assert.All(FmCalcCatalog.ControlForms, c =>
            Assert.Contains("${1:", c.Snippet));
    }

    [Fact]
    public void GrammarBuilder_ProducesValidJson()
    {
        var json = FmCalcGrammarBuilder.Build();
        var parsed = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("source.fmcalc", parsed.RootElement.GetProperty("scopeName").GetString());
    }

    [Fact]
    public void GrammarBuilder_IncludesEveryFunctionNameInItsCategoryRegex()
    {
        var json = FmCalcGrammarBuilder.Build();
        // Cheap check: every name should appear at least once in the JSON.
        // Misses would mean a function silently dropped out of the grammar.
        Assert.All(FmCalcCatalog.Functions, f => Assert.Contains(f.Name, json));
    }
}
