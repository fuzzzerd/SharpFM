using System;
using System.Collections.Generic;
using SharpFM.Model.Parsing;

namespace SharpFM.Model.Validation;

/// <summary>
/// Static registry of <see cref="IClipSemanticValidator"/>s the host runs after
/// every successful clip parse. <see cref="BuiltIns"/> is the production set;
/// the registry deliberately ships empty so domain rules can land one at a time
/// without bundling the framework PR.
/// </summary>
public static class SemanticValidatorRegistry
{
    [ThreadStatic]
    private static IReadOnlyList<IClipSemanticValidator>? _overrideForTest;

    /// <summary>Validators that run on every parse in production.</summary>
    public static IReadOnlyList<IClipSemanticValidator> BuiltIns { get; } = [];

    /// <summary>
    /// Run the built-in validators for <paramref name="formatId"/> against
    /// <paramref name="model"/> and return the combined diagnostics. Returns
    /// a shared empty list when nothing matches.
    /// </summary>
    public static IReadOnlyList<ClipParseDiagnostic> Run(string formatId, ClipModel model) =>
        Run(formatId, model, _overrideForTest ?? BuiltIns);

    /// <summary>
    /// Per-thread override of <see cref="BuiltIns"/> for integration tests
    /// that need to assert validators actually run through the parse pipeline.
    /// Dispose to restore. Production code never sets this.
    /// </summary>
    internal static IDisposable OverrideForTest(IReadOnlyList<IClipSemanticValidator> validators)
    {
        _overrideForTest = validators;
        return new Resetter();
    }

    private sealed class Resetter : IDisposable
    {
        public void Dispose() => _overrideForTest = null;
    }

    /// <summary>
    /// Same as <see cref="Run(string, ClipModel)"/> but with an explicit
    /// validator set — exposed for tests so the public <see cref="BuiltIns"/>
    /// stays static and immutable.
    /// </summary>
    internal static IReadOnlyList<ClipParseDiagnostic> Run(
        string formatId,
        ClipModel model,
        IReadOnlyList<IClipSemanticValidator> validators)
    {
        if (validators.Count == 0)
        {
            return Array.Empty<ClipParseDiagnostic>();
        }

        List<ClipParseDiagnostic>? acc = null;
        foreach (var v in validators)
        {
            if (!Applies(v, formatId))
            {
                continue;
            }

            var diags = v.Validate(model);
            if (diags.Count == 0)
            {
                continue;
            }

            (acc ??= []).AddRange(diags);
        }
        return (IReadOnlyList<ClipParseDiagnostic>?)acc ?? Array.Empty<ClipParseDiagnostic>();
    }

    private static bool Applies(IClipSemanticValidator validator, string formatId)
    {
        foreach (var id in validator.FormatIds)
        {
            if (id == IClipSemanticValidator.AllFormats || id == formatId)
            {
                return true;
            }
        }
        return false;
    }
}
