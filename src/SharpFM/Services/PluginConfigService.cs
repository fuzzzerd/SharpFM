using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpFM.Plugin;

namespace SharpFM.Services;

/// <summary>
/// Persists plugin configuration values as per-plugin JSON files under
/// <c>%LocalAppData%/SharpFM/plugin-config/</c>. The service validates and coerces
/// values against the caller-supplied <see cref="PluginConfigSchema"/> on every load
/// so a drifted or hand-edited file can never crash the host.
/// </summary>
public class PluginConfigService
{
    private readonly ILogger _logger;

    public string ConfigDirectory { get; }

    public PluginConfigService(ILogger logger)
        : this(logger, DefaultDirectory())
    {
    }

    public PluginConfigService(ILogger logger, string configDirectory)
    {
        _logger = logger;
        ConfigDirectory = configDirectory;
    }

    private static string DefaultDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SharpFM", "plugin-config");

    /// <summary>
    /// Load configuration values for <paramref name="pluginId"/> against
    /// <paramref name="schema"/>. Missing, malformed, or type-incompatible values
    /// fall back to the field's <see cref="PluginConfigField.DefaultValue"/>. Keys in
    /// the file that are not in the schema are dropped.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Load(string pluginId, PluginConfigSchema schema)
    {
        var path = GetConfigPath(pluginId);
        Dictionary<string, JsonElement>? raw = null;
        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Malformed plugin config at {Path}; falling back to defaults.", path);
            }
        }

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in schema.Fields)
        {
            if (raw is not null && raw.TryGetValue(field.Key, out var element)
                && TryCoerce(element, field, out var coerced))
            {
                result[field.Key] = coerced;
            }
            else
            {
                if (raw is not null && raw.ContainsKey(field.Key))
                {
                    _logger.LogWarning(
                        "Plugin config field {Key} for {PluginId} has an invalid value; using default.",
                        field.Key, pluginId);
                }
                result[field.Key] = field.DefaultValue;
            }
        }
        return result;
    }

    /// <summary>
    /// Load the persisted (or default) values for <paramref name="plugin"/> and push
    /// them into <see cref="IPlugin.OnConfigChanged"/>. No-op if the plugin declares
    /// an empty schema. Exceptions from the plugin are caught and logged so one bad
    /// plugin cannot abort host startup.
    /// </summary>
    public void Apply(IPlugin plugin)
    {
        var schema = plugin.ConfigSchema;
        if (schema.Fields.Count == 0) return;

        var values = Load(plugin.Id, schema);
        try
        {
            plugin.OnConfigChanged(values);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plugin {Id} threw from OnConfigChanged; continuing.", plugin.Id);
        }
    }

    /// <summary>
    /// Persist <paramref name="values"/> for <paramref name="pluginId"/>. Only keys
    /// declared in <paramref name="schema"/> are written — extra keys are dropped.
    /// </summary>
    public void Save(string pluginId, PluginConfigSchema schema, IReadOnlyDictionary<string, object?> values)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var toWrite = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in schema.Fields)
        {
            if (values.TryGetValue(field.Key, out var v))
                toWrite[field.Key] = v;
        }

        var path = GetConfigPath(pluginId);
        var text = JsonSerializer.Serialize(toWrite, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, text);
    }

    /// <summary>Path to the config file for a given plugin id. Sanitized so that
    /// ids containing path separators or other unsafe characters cannot escape
    /// <see cref="ConfigDirectory"/>.</summary>
    public string GetConfigPath(string pluginId)
    {
        var safe = SanitizeId(pluginId);
        return Path.Combine(ConfigDirectory, safe + ".json");
    }

    private static string SanitizeId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return "_";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = pluginId.Select(c =>
            invalid.Contains(c) || c == '.' || c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar
                ? '_' : c).ToArray();
        var result = new string(chars).Trim('.', '_', ' ');
        return string.IsNullOrEmpty(result) ? "_" : result;
    }

    private static bool TryCoerce(JsonElement element, PluginConfigField field, out object? value)
    {
        value = null;
        try
        {
            switch (field.Type)
            {
                case PluginConfigFieldType.String:
                case PluginConfigFieldType.MultilineString:
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        value = element.GetString();
                        return true;
                    }
                    return false;
                case PluginConfigFieldType.Bool:
                    if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        value = element.GetBoolean();
                        return true;
                    }
                    return false;
                case PluginConfigFieldType.Int:
                    if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var i))
                    {
                        value = i;
                        return true;
                    }
                    return false;
                case PluginConfigFieldType.Double:
                    if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var d))
                    {
                        value = d;
                        return true;
                    }
                    return false;
                case PluginConfigFieldType.Enum:
                    if (element.ValueKind != JsonValueKind.String) return false;
                    var s = element.GetString();
                    if (field.EnumValues is null || !field.EnumValues.Contains(s)) return false;
                    value = s;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
