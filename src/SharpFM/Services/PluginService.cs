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
/// Discovers, loads, and manages plugin implementations from the plugins/ directory.
/// Supports all plugin types: panel, event, persistence, and transform.
/// </summary>
public class PluginService
{
    private readonly ILogger _logger;
    private readonly List<IPanelPlugin> _panelPlugins = [];
    private readonly List<IEventPlugin> _eventPlugins = [];
    private readonly List<IPersistencePlugin> _persistencePlugins = [];
    private readonly List<IClipTransformPlugin> _transformPlugins = [];

    /// <summary>Panel plugins that provide sidebar UI.</summary>
    public IReadOnlyList<IPanelPlugin> PanelPlugins => _panelPlugins;

    /// <summary>Headless event handler plugins.</summary>
    public IReadOnlyList<IEventPlugin> EventPlugins => _eventPlugins;

    /// <summary>Storage backend plugins.</summary>
    public IReadOnlyList<IPersistencePlugin> PersistencePlugins => _persistencePlugins;

    /// <summary>Clip transform plugins for import/export pipeline.</summary>
    public IReadOnlyList<IClipTransformPlugin> TransformPlugins => _transformPlugins;

    /// <summary>All loaded plugins across all types.</summary>
    public IReadOnlyList<IPlugin> AllPlugins =>
        [.. _panelPlugins, .. _eventPlugins, .. _persistencePlugins, .. _transformPlugins];

    /// <summary>Backwards compat: returns panel plugins only.</summary>
    public IReadOnlyList<IPanelPlugin> LoadedPlugins => _panelPlugins;

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

        _logger.LogInformation(
            "Loaded {Count} plugin(s): {Panel} panel, {Event} event, {Persistence} persistence, {Transform} transform.",
            AllPlugins.Count, _panelPlugins.Count, _eventPlugins.Count,
            _persistencePlugins.Count, _transformPlugins.Count);
    }

    /// <summary>
    /// Copy a plugin DLL into the plugins directory and load it.
    /// Returns all newly loaded plugins, or empty if it failed or contained none.
    /// </summary>
    public IReadOnlyList<IPlugin> InstallPlugin(string sourceDllPath, IPluginHost host)
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

        var beforeCount = AllPlugins.Count;
        try
        {
            LoadPluginAssembly(destPath, host);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load installed plugin {Path}.", destPath);
            return [];
        }

        return AllPlugins.Skip(beforeCount).ToList();
    }

    /// <summary>
    /// Remove a plugin DLL from the plugins directory.
    /// The plugin remains in memory until the app restarts.
    /// </summary>
    public bool UninstallPlugin(IPlugin plugin)
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
            RemoveFromTypedList(plugin);
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

    private string? FindPluginDll(IPlugin plugin)
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

    private void RemoveFromTypedList(IPlugin plugin)
    {
        switch (plugin)
        {
            case IPanelPlugin p: _panelPlugins.Remove(p); break;
            case IEventPlugin p: _eventPlugins.Remove(p); break;
            case IPersistencePlugin p: _persistencePlugins.Remove(p); break;
            case IClipTransformPlugin p: _transformPlugins.Remove(p); break;
        }
    }

    private void LoadPluginAssembly(string dllPath, IPluginHost host)
    {
        var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dllPath), isCollectible: false);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

        var candidateTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in candidateTypes)
        {
            if (Activator.CreateInstance(type) is not IPlugin plugin) continue;

            plugin.Initialize(host);

            switch (plugin)
            {
                case IPanelPlugin p:
                    _panelPlugins.Add(p);
                    break;
                case IEventPlugin p:
                    _eventPlugins.Add(p);
                    break;
                case IPersistencePlugin p:
                    _persistencePlugins.Add(p);
                    break;
                case IClipTransformPlugin p:
                    _transformPlugins.Add(p);
                    break;
                default:
                    _logger.LogWarning("Plugin {Id} implements IPlugin but no known subtype, skipping.", plugin.Id);
                    plugin.Dispose();
                    continue;
            }

            _logger.LogInformation("Loaded {Type} plugin: {Id} ({DisplayName})",
                plugin.GetType().BaseType?.Name ?? plugin.GetType().Name, plugin.Id, plugin.DisplayName);
        }
    }
}
