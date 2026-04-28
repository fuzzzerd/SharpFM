using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Static, explicitly-populated registry of <see cref="IClipTypeStrategy"/>
/// implementations keyed by <c>Mac-XM*</c> format id. Built-in strategies are
/// registered once at startup via <see cref="RegisterBuiltIns"/>; tests
/// reset and re-register through <see cref="Reset"/>.
/// </summary>
/// <remarks>
/// Reflection-based auto-discovery (the pattern used by <c>StepRegistry</c>)
/// is deliberately not used here — clip types are few, low-cardinality, and
/// explicit registration makes the bootstrapping order obvious.
/// </remarks>
public static class ClipTypeRegistry
{
    private static readonly object _gate = new();
    private static readonly Dictionary<string, IClipTypeStrategy> _strategies = new();

    /// <summary>Register a strategy. A duplicate <see cref="IClipTypeStrategy.FormatId"/> overwrites the prior entry.</summary>
    public static void Register(IClipTypeStrategy strategy)
    {
        lock (_gate)
        {
            _strategies[strategy.FormatId] = strategy;
        }
    }

    /// <summary>
    /// Resolve a strategy for the given format id. Unknown ids fall back to
    /// <see cref="OpaqueClipStrategy.Instance"/> so callers always receive a
    /// usable strategy.
    /// </summary>
    public static IClipTypeStrategy For(string formatId)
    {
        lock (_gate)
        {
            return _strategies.TryGetValue(formatId, out var strategy)
                ? strategy
                : OpaqueClipStrategy.Instance;
        }
    }

    /// <summary>True if the given format id has a dedicated strategy registered.</summary>
    public static bool IsRegistered(string formatId)
    {
        lock (_gate)
        {
            return _strategies.ContainsKey(formatId);
        }
    }

    /// <summary>All explicitly-registered strategies, in registration order.</summary>
    public static IReadOnlyList<IClipTypeStrategy> All
    {
        get
        {
            lock (_gate)
            {
                return _strategies.Values.ToList();
            }
        }
    }

    /// <summary>
    /// Register every built-in clip-type strategy. Called once at host startup;
    /// idempotent thanks to <see cref="Register"/>'s overwrite semantics.
    /// </summary>
    public static void RegisterBuiltIns()
    {
        // Built-in strategies are registered here as they land in subsequent
        // commits (script, table, layout). Opaque is the implicit fallback;
        // it is not registered.
    }

    /// <summary>Clear the registry. Tests use this to isolate from production registrations.</summary>
    internal static void Reset()
    {
        lock (_gate)
        {
            _strategies.Clear();
        }
    }
}
