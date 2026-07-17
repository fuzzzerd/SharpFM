using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Cached property access used by the shape-driven renderer/parser to bind a
/// <c>ShapeNode</c> to a POCO (or discriminated-union variant) property by
/// name. Reflection cost is paid once per (type, property) pair.
/// </summary>
internal static class ShapeReflection
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> _cache = new();

    private static PropertyInfo Prop(Type type, string name) =>
        _cache.GetOrAdd((type, name), key =>
            key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Shape references property '{key.Item2}' which does not exist on {key.Item1.Name}."));

    public static object? Get(object source, string name) =>
        Prop(source.GetType(), name).GetValue(source);

    /// <summary>
    /// Writes a shape-bound property, including init-only setters on record
    /// positional parameters. A bound property with no setter marks an
    /// emit-only node (e.g. a variant discriminator bound to a computed
    /// <c>WireValue</c>), so parsing skips it rather than throwing.
    /// </summary>
    public static void Set(object target, string name, object? value)
    {
        var prop = Prop(target.GetType(), name);
        if (prop.CanWrite)
            prop.SetValue(target, value);
    }

    /// <summary>Declared type of a shape-bound property (for typed list parsing).</summary>
    public static Type PropertyType(object source, string name) =>
        Prop(source.GetType(), name).PropertyType;

    /// <summary>
    /// True when the shape-bound property has a setter. False marks an
    /// emit-only projection (e.g. a wire-order alias of another property),
    /// which callers that need the write to actually land must route around.
    /// </summary>
    public static bool CanWrite(object target, string name) =>
        Prop(target.GetType(), name).CanWrite;
}
