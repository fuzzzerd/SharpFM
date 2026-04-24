using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace SharpFM.Scripting.Editor;

[ExcludeFromCodeCoverage]
public class FmScriptRegistryOptions : IRegistryOptions
{
    public const string ScopeName = "source.fmscript";

    private readonly RegistryOptions _inner;

    // The embedded .tmLanguage.json is immutable; parsing it is not free. Cache
    // the parsed grammar once per process so repeated TextMate installs (e.g.
    // fresh script-editor instances) don't reparse ~KB of JSON each time.
    private static readonly Lazy<IRawGrammar> CachedGrammar =
        new(LoadFmScriptGrammar, LazyThreadSafetyMode.ExecutionAndPublication);

    public FmScriptRegistryOptions(RegistryOptions inner)
    {
        _inner = inner;
    }

    public IRawTheme GetDefaultTheme() => _inner.GetDefaultTheme();

    public IRawTheme GetTheme(string scopeName) => _inner.GetTheme(scopeName);

    public ICollection<string> GetInjections(string scopeName) => _inner.GetInjections(scopeName);

    public IRawGrammar GetGrammar(string scopeName)
    {
        if (scopeName == ScopeName)
            return CachedGrammar.Value;

        return _inner.GetGrammar(scopeName);
    }

    private static IRawGrammar LoadFmScriptGrammar()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            [System.Array.FindIndex(assembly.GetManifestResourceNames(),
                n => n.EndsWith("fmscript.tmLanguage.json"))];

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return GrammarReader.ReadGrammarSync(reader);
    }
}
