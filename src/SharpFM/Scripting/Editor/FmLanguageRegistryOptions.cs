using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using SharpFM.Model.Scripting.Calc;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Serves the FileMaker TextMate grammars (<c>source.fmscript</c> and
/// <c>source.fmcalc</c>) and delegates everything else to an inner
/// <see cref="RegistryOptions"/>. The script grammar is hand-authored and
/// embedded as a resource; the calc grammar is built at first use from
/// <see cref="FmCalcCatalog"/> via <see cref="FmCalcGrammarBuilder"/> so the
/// catalog is the single source of truth for both grammar and completions.
/// Cross-grammar <c>include</c>s — e.g. the script grammar embedding the
/// calc grammar inside <c>[ ... ]</c> — resolve through this method.
/// </summary>
[ExcludeFromCodeCoverage]
public class FmLanguageRegistryOptions : IRegistryOptions
{
    public const string ScriptScopeName = "source.fmscript";
    public const string CalcScopeName = "source.fmcalc";

    private readonly RegistryOptions _inner;

    private static readonly Lazy<IRawGrammar> ScriptGrammar =
        new(LoadEmbeddedScriptGrammar, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<IRawGrammar> CalcGrammar =
        new(BuildCalcGrammar, LazyThreadSafetyMode.ExecutionAndPublication);

    public FmLanguageRegistryOptions(RegistryOptions inner)
    {
        _inner = inner;
    }

    public IRawTheme GetDefaultTheme() => _inner.GetDefaultTheme();

    public IRawTheme GetTheme(string scopeName) => _inner.GetTheme(scopeName);

    public ICollection<string> GetInjections(string scopeName) => _inner.GetInjections(scopeName);

    public IRawGrammar GetGrammar(string scopeName) => scopeName switch
    {
        ScriptScopeName => ScriptGrammar.Value,
        CalcScopeName => CalcGrammar.Value,
        _ => _inner.GetGrammar(scopeName),
    };

    private static IRawGrammar LoadEmbeddedScriptGrammar()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("fmscript.tmLanguage.json", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Embedded fmscript grammar resource not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return GrammarReader.ReadGrammarSync(reader);
    }

    private static IRawGrammar BuildCalcGrammar()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(FmCalcGrammarBuilder.Build());
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);
        return GrammarReader.ReadGrammarSync(reader);
    }
}
