using System.Linq;
using SharpFM.Model.Scripting.Calc;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class FmCalcSignatureParserTests
{
    [Fact]
    public void SinglePositional_ParsesOneParam()
    {
        var ps = FmCalcSignatureParser.ParseParams("Length(text)");
        Assert.Equal(new[] { "text" }, ps.Select(p => p.Name));
    }

    [Fact]
    public void MultiplePositional_ParsesEach()
    {
        var ps = FmCalcSignatureParser.ParseParams("Replace(text; start; numberOfCharacters; replacementText)");
        Assert.Equal(
            new[] { "text", "start", "numberOfCharacters", "replacementText" },
            ps.Select(p => p.Name));
    }

    [Fact]
    public void VariadicTrailing_StopsAtBrace()
    {
        var ps = FmCalcSignatureParser.ParseParams("Average(field {; field...})");
        Assert.Equal(new[] { "field" }, ps.Select(p => p.Name));
    }

    [Fact]
    public void NoParens_ReturnsEmpty()
    {
        // E.g. Random — written without parens in the catalog.
        Assert.Empty(FmCalcSignatureParser.ParseParams("Random"));
    }

    [Fact]
    public void EmptyParens_ReturnsEmpty()
    {
        Assert.Empty(FmCalcSignatureParser.ParseParams("Foo()"));
    }

    [Fact]
    public void TrimsWhitespace()
    {
        var ps = FmCalcSignatureParser.ParseParams("Round(  number ;  precision )");
        Assert.Equal(new[] { "number", "precision" }, ps.Select(p => p.Name));
    }

    [Fact]
    public void Catalog_EveryFunctionWithParensInSignature_HasAtLeastOneParam()
    {
        // The catalog's Add helper now derives Params from Signature when
        // none are passed explicitly. This guards against a function whose
        // signature contains `(...)` but somehow ends up with empty Params.
        // Skip signatures whose only content is an optional region — e.g.
        // WindowNames({fileName}) has no required positional params, so 0
        // is correct.
        var bad = FmCalcCatalog.Functions
            .Where(f =>
            {
                if (!f.Signature.Contains('(')) return false;
                if (f.Signature.EndsWith("()")) return false;
                var open = f.Signature.IndexOf('(');
                var inner = f.Signature.Substring(open + 1).TrimEnd(')');
                if (inner.TrimStart().StartsWith("{")) return false;
                return f.Params.Count == 0;
            })
            .Select(f => f.Name)
            .ToList();
        Assert.Empty(bad);
    }

    [Fact]
    public void Catalog_JsonGetElement_HasJsonAndPathParams()
    {
        // JSONGetElement was the user-flagged regression — accepting it
        // should give tab-stops, which requires Params from the parser.
        var fn = FmCalcCatalog.Functions.First(f => f.Name == "JSONGetElement");
        Assert.Equal(new[] { "json", "keyOrIndexOrPath" }, fn.Params.Select(p => p.Name));
    }

    [Fact]
    public void Catalog_ExplicitParamsTakePrecedenceOverSignature()
    {
        // Get's signature reads "Get(parameter)", but the catalog passes
        // an explicit param with ValidValues. Verify the explicit one
        // survived rather than being clobbered by the signature parse.
        var fn = FmCalcCatalog.Functions.First(f => f.Name == "Get");
        Assert.Single(fn.Params);
        Assert.NotNull(fn.Params[0].ValidValues);
        Assert.Contains(fn.Params[0].ValidValues!, v => v.Name == "AccountName");
    }
}
