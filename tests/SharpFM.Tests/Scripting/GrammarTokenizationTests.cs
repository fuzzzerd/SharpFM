using System.Linq;
using SharpFM.Scripting.Editor;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

/// <summary>
/// Loads the embedded FileMaker TextMate grammars through a real TextMateSharp
/// Registry and asserts representative fixtures tokenize as expected. Guards
/// against silent regressions from grammar edits, function-list churn, or
/// cross-grammar include resolution failing.
/// </summary>
public class GrammarTokenizationTests
{
    private static IGrammar LoadGrammar(string scopeName)
    {
        var options = new FmLanguageRegistryOptions(new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus));
        var registry = new Registry(options);
        return registry.LoadGrammar(scopeName);
    }

    private static string[] ScopesAt(IGrammar grammar, string line, int column)
    {
        var result = grammar.TokenizeLine(line);
        var token = result.Tokens.First(t => column >= t.StartIndex && column < t.EndIndex);
        return token.Scopes.ToArray();
    }

    private static bool LineHasScope(IGrammar grammar, string line, string scope)
    {
        var result = grammar.TokenizeLine(line);
        return result.Tokens.Any(t => t.Scopes.Any(s => s == scope || s.StartsWith(scope + ".")));
    }

    [Fact]
    public void FmCalc_LineComment_IsScopedAsComment()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "// hello", 3), s => s.StartsWith("comment.line"));
    }

    [Fact]
    public void FmCalc_BlockComment_IsScopedAsBlockComment()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "/* note */ 1", 4), s => s.StartsWith("comment.block"));
    }

    [Fact]
    public void FmCalc_StringWithEscape_HasEscapeScope()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        var line = "\"a\\\"b\"";
        // Position of the backslash escape (index 2)
        Assert.Contains(ScopesAt(g, line, 2), s => s.Contains("constant.character.escape"));
    }

    [Fact]
    public void FmCalc_NumericLiteral_IsConstantNumeric()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "1.5e3", 0), s => s.StartsWith("constant.numeric"));
    }

    [Fact]
    public void FmCalc_LetControlForm_IsKeywordControl()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "Let ( x = 1 ; x )", 0), s => s.StartsWith("keyword.control"));
    }

    [Fact]
    public void FmCalc_NestedLet_BothLetTokensAreKeywords()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        var line = "Let([a=1;b=Let([c=2];c)];a+b)";
        Assert.Contains(ScopesAt(g, line, 0), s => s.StartsWith("keyword.control"));
        Assert.Contains(ScopesAt(g, line, 11), s => s.StartsWith("keyword.control"));
    }

    [Fact]
    public void FmCalc_FieldReference_TablePartIsEntityName()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "Customer::Name", 0), s => s.StartsWith("entity.name.type"));
        Assert.Contains(ScopesAt(g, "Customer::Name", 10), s => s.StartsWith("variable.other.member"));
    }

    [Fact]
    public void FmCalc_DollarVariable_IsVariableScope()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "$myVar + $$global", 0), s => s.StartsWith("variable.other"));
        Assert.Contains(ScopesAt(g, "$myVar + $$global", 9), s => s.StartsWith("variable.other"));
    }

    [Fact]
    public void FmCalc_BuiltinFunction_HasCategoryScope()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "Length ( name )", 0), s => s.Contains("support.function.text"));
        Assert.Contains(ScopesAt(g, "JSONGetElement ( j ; \"k\" )", 0), s => s.Contains("support.function.json"));
    }

    [Fact]
    public void FmCalc_CustomFunction_IsEntityNameFunction()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "MyCustomFn ( 1 )", 0), s => s.StartsWith("entity.name.function"));
    }

    [Fact]
    public void FmCalc_BlockComment_SpansMultipleLines()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        var first = g.TokenizeLine("/* opens");
        Assert.Contains(first.Tokens, t => t.Scopes.Any(s => s.StartsWith("comment.block")));
        var second = g.TokenizeLine("still inside */", first.RuleStack, System.TimeSpan.MaxValue);
        Assert.True(LineHasScope(g, "still inside", "comment.block") ||
                    second.Tokens.Any(t => t.Scopes.Any(s => s.StartsWith("comment.block"))));
    }

    [Fact]
    public void FmCalc_BooleanConstants_AreConstantLanguage()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.CalcScopeName);
        Assert.Contains(ScopesAt(g, "True", 0), s => s.StartsWith("constant.language"));
        Assert.Contains(ScopesAt(g, "False", 0), s => s.StartsWith("constant.language"));
        Assert.Contains(ScopesAt(g, "Pi", 0), s => s.StartsWith("constant.language"));
    }

    [Fact]
    public void FmScript_StepName_IsEntityNameFunction()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.ScriptScopeName);
        Assert.Contains(ScopesAt(g, "Set Variable [ $x ; Value: 1 ]", 0), s => s.StartsWith("entity.name.function"));
    }

    [Fact]
    public void FmScript_BracketParams_FieldRefUsesScriptScope()
    {
        // The script grammar's bracket-params highlight calc fragments
        // with its own native scopes — it intentionally does NOT include
        // source.fmcalc, since the embedded grammar's pattern fan-out is
        // measurably expensive on long scripts. The calc editor still
        // uses source.fmcalc for full per-category function highlighting.
        var g = LoadGrammar(FmLanguageRegistryOptions.ScriptScopeName);
        var line = "Set Field [ Customer::Name ; \"Bob\" ]";
        Assert.True(LineHasScope(g, line, "entity.name.type.fmscript"));
        Assert.True(LineHasScope(g, line, "string.quoted.double.fmscript"));
    }

    [Fact]
    public void FmScript_BracketParams_FunctionAndVariableUseScriptScope()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.ScriptScopeName);
        var line = "Set Variable [ $x ; Value: Length ( $name ) ]";
        Assert.True(LineHasScope(g, line, "support.function.fmscript"));
        Assert.True(LineHasScope(g, line, "variable.other.fmscript"));
    }

    [Fact]
    public void FmScript_ParamLabel_IsScriptScope()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.ScriptScopeName);
        Assert.True(LineHasScope(g, "Set Variable [ $x ; Value: 1 ]", "support.type.property-name.fmscript"));
    }

    [Fact]
    public void FmScript_HashCommentLine_IsScriptComment()
    {
        var g = LoadGrammar(FmLanguageRegistryOptions.ScriptScopeName);
        Assert.True(LineHasScope(g, "# this is a script comment", "comment.line.number-sign.fmscript"));
    }
}
