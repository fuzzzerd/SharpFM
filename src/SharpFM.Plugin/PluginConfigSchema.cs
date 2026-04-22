using System;
using System.Collections.Generic;

namespace SharpFM.Plugin;

/// <summary>
/// Supported field types for a plugin configuration schema. The host renders a
/// different control per type and coerces persisted values to the matching CLR type.
/// </summary>
public enum PluginConfigFieldType
{
    String,
    MultilineString,
    Bool,
    Int,
    Double,
    Enum
}

/// <summary>
/// A single configurable value declared by a plugin.
/// </summary>
/// <param name="Key">Dictionary key used when values are passed back to the plugin.</param>
/// <param name="Label">Human-readable label shown in the settings UI.</param>
/// <param name="Type">Field type — determines the rendered control and value coercion.</param>
/// <param name="DefaultValue">
/// Value returned when the field has never been set or the persisted value is invalid.
/// Must be assignable to the CLR type implied by <paramref name="Type"/>.
/// </param>
/// <param name="Description">Optional caption shown beneath the control.</param>
/// <param name="EnumValues">Allowed values when <paramref name="Type"/> is <see cref="PluginConfigFieldType.Enum"/>.</param>
public sealed record PluginConfigField(
    string Key,
    string Label,
    PluginConfigFieldType Type,
    object? DefaultValue = null,
    string? Description = null,
    IReadOnlyList<string>? EnumValues = null);

/// <summary>
/// Describes a plugin's user-tunable configuration. The host uses this to
/// persist values, generate a settings UI, and push the current values into the plugin.
/// </summary>
public sealed record PluginConfigSchema(IReadOnlyList<PluginConfigField> Fields)
{
    /// <summary>Schema for plugins with no configuration.</summary>
    public static PluginConfigSchema Empty { get; } = new(Array.Empty<PluginConfigField>());
}
