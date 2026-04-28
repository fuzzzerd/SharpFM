using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Compile-time registry of <see cref="IClipTypeStrategy"/> implementations
/// keyed by <c>Mac-XM*</c> format id. Strategies are fully owned by SharpFM
/// (no plugin extension point), so the table is built once from a static
/// list and is read-only thereafter — no locks, no bootstrap, no reset.
/// Adding a new clip type means writing the strategy and adding it to
/// <see cref="BuiltIns"/>.
/// </summary>
public static class ClipTypeRegistry
{
    public static IReadOnlyList<IClipTypeStrategy> BuiltIns { get; } =
    [
        ScriptClipStrategy.Steps,
        ScriptClipStrategy.Script,
        TableClipStrategy.Table,
        TableClipStrategy.Field,
        LayoutClipStrategy.Instance,
    ];

    private static readonly Dictionary<string, IClipTypeStrategy> _byFormatId =
        BuiltIns.ToDictionary(s => s.FormatId);

    /// <summary>All built-in strategies (excludes the opaque fallback).</summary>
    public static IReadOnlyList<IClipTypeStrategy> All => BuiltIns;

    /// <summary>
    /// Resolve a strategy for the given format id. Unknown ids fall back to
    /// <see cref="OpaqueClipStrategy.Instance"/> so callers always receive a
    /// usable strategy.
    /// </summary>
    public static IClipTypeStrategy For(string formatId) =>
        _byFormatId.TryGetValue(formatId, out var strategy)
            ? strategy
            : OpaqueClipStrategy.Instance;

    /// <summary>True if the given format id has a dedicated built-in strategy.</summary>
    public static bool IsRegistered(string formatId) =>
        _byFormatId.ContainsKey(formatId);
}
