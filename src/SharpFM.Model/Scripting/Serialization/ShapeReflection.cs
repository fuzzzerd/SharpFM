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

    public static void Set(object target, string name, object? value) =>
        Prop(target.GetType(), name).SetValue(target, value);
}
