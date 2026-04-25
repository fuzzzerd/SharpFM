using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Serves the embedded FileMaker TextMate grammars (<c>source.fmscript</c>
/// and <c>source.fmcalc</c>) and delegates everything else to an inner
/// <see cref="RegistryOptions"/>. Cross-grammar <c>include</c>s — e.g. the
/// script grammar embedding the calc grammar inside <c>[ ... ]</c> — resolve
/// through this method.
/// </summary>
[ExcludeFromCodeCoverage]
public class FmLanguageRegistryOptions : IRegistryOptions
{
    public const string ScriptScopeName = "source.fmscript";
    public const string CalcScopeName = "source.fmcalc";

    private readonly RegistryOptions _inner;

    private static readonly Lazy<IRawGrammar> ScriptGrammar =
        new(() => LoadGrammar("fmscript.tmLanguage.json"), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<IRawGrammar> CalcGrammar =
        new(() => LoadGrammar("fmcalc.tmLanguage.json"), LazyThreadSafetyMode.ExecutionAndPublication);

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

    private static IRawGrammar LoadGrammar(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded grammar resource not found: {fileName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return GrammarReader.ReadGrammarSync(reader);
    }
}
