using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using SharpFM.Plugin;

namespace SharpFM.Services;

/// <summary>
/// Discovers, loads, and manages <see cref="IPanelPlugin"/> implementations from the plugins/ directory.
/// </summary>
public class PluginService
{
    private readonly ILogger _logger;
    private readonly List<IPanelPlugin> _loadedPlugins = [];

    public IReadOnlyList<IPanelPlugin> LoadedPlugins => _loadedPlugins;

    public string PluginsDirectory { get; }

    public PluginService(ILogger logger)
        : this(logger, Path.Combine(AppContext.BaseDirectory, "plugins"))
    {
    }

    internal PluginService(ILogger logger, string pluginsDirectory)
    {
        _logger = logger;
        PluginsDirectory = pluginsDirectory;
    }

    public void LoadPlugins(IPluginHost host)
    {
        if (!Directory.Exists(PluginsDirectory))
        {
            _logger.LogInformation("No plugins directory found at {Path}, skipping plugin load.", PluginsDirectory);
            return;
        }

        foreach (var dll in Directory.EnumerateFiles(PluginsDirectory, "*.dll"))
        {
            try
            {
                LoadPluginAssembly(dll, host);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin from {Path}.", dll);
            }
        }

        _logger.LogInformation("Loaded {Count} plugin(s).", _loadedPlugins.Count);
    }

    /// <summary>
    /// Copy a plugin DLL into the plugins directory and load it.
    /// Returns the loaded plugin(s), or empty if it failed or contained no plugins.
    /// </summary>
    public IReadOnlyList<IPanelPlugin> InstallPlugin(string sourceDllPath, IPluginHost host)
    {
        Directory.CreateDirectory(PluginsDirectory);
        var fileName = Path.GetFileName(sourceDllPath);
        var destPath = Path.Combine(PluginsDirectory, fileName);

        if (File.Exists(destPath))
        {
            _logger.LogWarning("Plugin file {FileName} already exists, overwriting.", fileName);
        }

        File.Copy(sourceDllPath, destPath, overwrite: true);
        _logger.LogInformation("Copied plugin to {Path}.", destPath);

        var before = _loadedPlugins.Count;
        try
        {
            LoadPluginAssembly(destPath, host);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load installed plugin {Path}.", destPath);
            return [];
        }

        return _loadedPlugins.Skip(before).ToList();
    }

    /// <summary>
    /// Remove a plugin DLL from the plugins directory.
    /// The plugin remains in memory until the app restarts.
    /// </summary>
    public bool UninstallPlugin(IPanelPlugin plugin)
    {
        var dllPath = FindPluginDll(plugin);
        if (dllPath is null)
        {
            _logger.LogWarning("Could not find DLL for plugin {Id}.", plugin.Id);
            return false;
        }

        try
        {
            plugin.Dispose();
            _loadedPlugins.Remove(plugin);
            File.Delete(dllPath);
            _logger.LogInformation("Uninstalled plugin {Id} from {Path}.", plugin.Id, dllPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to uninstall plugin {Id}.", plugin.Id);
            return false;
        }
    }

    /// <summary>
    /// Get the list of DLL files in the plugins directory (for UI display).
    /// </summary>
    public IReadOnlyList<string> GetInstalledPluginFiles()
    {
        if (!Directory.Exists(PluginsDirectory))
            return [];

        return Directory.EnumerateFiles(PluginsDirectory, "*.dll").ToList();
    }

    private string? FindPluginDll(IPanelPlugin plugin)
    {
        var assembly = plugin.GetType().Assembly;
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
            return location;

        // Fallback: search by assembly name
        var name = assembly.GetName().Name + ".dll";
        var candidate = Path.Combine(PluginsDirectory, name);
        return File.Exists(candidate) ? candidate : null;
    }

    private void LoadPluginAssembly(string dllPath, IPluginHost host)
    {
        var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dllPath), isCollectible: false);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPanelPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in pluginTypes)
        {
            if (Activator.CreateInstance(type) is not IPanelPlugin plugin) continue;

            plugin.Initialize(host);
            _loadedPlugins.Add(plugin);
            _logger.LogInformation("Loaded plugin: {Id} ({DisplayName})", plugin.Id, plugin.DisplayName);
        }
    }
}
