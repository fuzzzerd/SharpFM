using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Runtime registry of typed script-step POCOs. Populates itself on
/// first access (or an explicit <see cref="Initialize"/> call) by
/// reflecting over the model assembly for types that implement
/// <see cref="IStepFactory"/> and reading their static
/// <see cref="StepMetadata"/> property.
///
/// <para>
/// The registry bridges each POCO's factory delegates into
/// <see cref="StepXmlFactory"/> and <see cref="StepDisplayFactory"/> so
/// XML-parse and display-parse dispatch find the typed POCO without
/// each POCO needing its own <c>ModuleInitializer</c>.
/// </para>
/// </summary>
public static class StepRegistry
{
    private static readonly object _gate = new();
    private static bool _initialized;
    private static readonly List<StepMetadata> _all = [];
    private static readonly Dictionary<string, StepMetadata> _byName =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, StepMetadata> _byId = [];
    private static readonly Dictionary<Type, StepMetadata> _byType = [];

    /// <summary>
    /// All registered step metadata records, in discovery order.
    /// </summary>
    public static IReadOnlyList<StepMetadata> All
    {
        get { EnsureInitialized(); return _all; }
    }

    /// <summary>Case-insensitive lookup by canonical step name.</summary>
    public static IReadOnlyDictionary<string, StepMetadata> ByName
    {
        get { EnsureInitialized(); return _byName; }
    }

    /// <summary>Lookup by numeric FileMaker step id.</summary>
    public static IReadOnlyDictionary<int, StepMetadata> ById
    {
        get { EnsureInitialized(); return _byId; }
    }

    /// <summary>
    /// Returns the metadata associated with a step instance's runtime
    /// type, or <c>null</c> when the type is not a registered POCO
    /// (e.g. <see cref="RawStep"/>, which wraps unknown elements).
    /// </summary>
    public static StepMetadata? MetadataFor(ScriptStep step)
    {
        EnsureInitialized();
        return _byType.TryGetValue(step.GetType(), out var m) ? m : null;
    }

    /// <summary>
    /// Idempotent explicit initialization. Call at app startup to land
    /// the reflection cost at a predictable moment; otherwise the first
    /// lookup triggers the same scan as a safety net.
    /// </summary>
    public static void Initialize() => EnsureInitialized();

    /// <summary>
    /// Fires the registry scan at assembly-load time so POCO factories
    /// are bridged into the legacy <see cref="StepXmlFactory"/> /
    /// <see cref="StepDisplayFactory"/> surfaces before any consumer
    /// touches them — matching the ordering guarantee the legacy
    /// per-POCO <c>[ModuleInitializer]</c>s used to provide. The user
    /// preference is "no module initializers on POCOs"; one on the
    /// registry itself is the singular substitute.
    /// </summary>
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Single initializer on the registry populates legacy factories in place of per-POCO initializers.")]
    [ModuleInitializer]
    internal static void ModuleInitialize() => EnsureInitialized();

    /// <summary>
    /// Surface valid values for a parameter in completion / validation
    /// contexts. Returns the explicit <see cref="ParamMetadata.ValidValues"/>
    /// when present; otherwise defaults boolean-like params to
    /// <c>["On", "Off"]</c>.
    /// </summary>
    public static IReadOnlyList<string> GetValidValues(ParamMetadata param)
    {
        if (param.ValidValues is { Count: > 0 }) return param.ValidValues;
        if (param.Type is "boolean" or "flagBoolean" or "flagElement")
            return new[] { "On", "Off" };
        return [];
    }

    private static void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized)) return;
        lock (_gate)
        {
            if (_initialized) return;
            Scan();
            Volatile.Write(ref _initialized, true);
        }
    }

    private static void Scan()
    {
        var assembly = typeof(IStepFactory).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(IStepFactory).IsAssignableFrom(type)) continue;

            // Reflect the static Metadata property enforced by the interface.
            var prop = type.GetProperty(
                nameof(IStepFactory.Metadata),
                BindingFlags.Public | BindingFlags.Static);
            if (prop?.GetValue(null) is not StepMetadata metadata) continue;

            _all.Add(metadata);
            _byType[type] = metadata;
            if (!string.IsNullOrEmpty(metadata.Name))
                _byName[metadata.Name] = metadata;
            if (metadata.Id != 0)
                _byId[metadata.Id] = metadata;

            // Bridge into legacy factories so callers that still use
            // StepXmlFactory / StepDisplayFactory pick up POCO-backed
            // construction without each POCO needing a ModuleInitializer.
            if (metadata.FromXml is { } fromXml)
                StepXmlFactory.Register(metadata.Name, fromXml);
            if (metadata.FromDisplay is { } fromDisplay)
                StepDisplayFactory.Register(metadata.Name, fromDisplay);
        }

        // Sort for deterministic All iteration.
        _all.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }
}
