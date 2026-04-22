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
/// </summary>
public class PluginService
{
    private readonly ILogger _logger;
    private readonly PluginConfigService _configService;
    private readonly List<IPlugin> _plugins = [];

    /// <summary>All loaded plugins.</summary>
    public IReadOnlyList<IPlugin> AllPlugins => _plugins;

    public string PluginsDirectory { get; }

    public PluginService(ILogger logger, PluginConfigService configService)
        : this(logger, configService, Path.Combine(AppContext.BaseDirectory, "plugins"))
    {
    }

    public PluginService(ILogger logger, PluginConfigService configService, string pluginsDirectory)
    {
        _logger = logger;
        _configService = configService;
        PluginsDirectory = pluginsDirectory;
    }

    public void LoadPlugins(IPluginHost host)
    {
        if (!Directory.Exists(PluginsDirectory))
        {
            _logger.LogInformation("No plugins directory found at {Path}, skipping plugin load.", PluginsDirectory);
            return;
        }

        // Load plugins from flat DLLs in the plugins directory (legacy/simple plugins)
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

        // Load plugins from subdirectories: plugins/{Name}/{Name}.dll
        // This supports plugins that ship with dependencies and a .deps.json file.
        foreach (var subDir in Directory.EnumerateDirectories(PluginsDirectory))
        {
            var dirName = Path.GetFileName(subDir);
            var candidateDll = Path.Combine(subDir, dirName + ".dll");
            if (!File.Exists(candidateDll)) continue;

            try
            {
                LoadPluginAssembly(candidateDll, host);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin from {Path}.", candidateDll);
            }
        }

        _logger.LogInformation("Loaded {Count} plugin(s).", _plugins.Count);
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

        var beforeCount = _plugins.Count;
        try
        {
            LoadPluginAssembly(destPath, host);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load installed plugin {Path}.", destPath);
            return [];
        }

        return _plugins.Skip(beforeCount).ToList();
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
            _plugins.Remove(plugin);
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

    private void LoadPluginAssembly(string dllPath, IPluginHost host)
    {
        var fullPath = Path.GetFullPath(dllPath);
        var pluginDir = Path.GetDirectoryName(fullPath)!;
        var context = new AssemblyLoadContext(
            Path.GetFileNameWithoutExtension(dllPath), isCollectible: false);

        // Resolving fires only AFTER the default ALC fails to find an assembly.
        // This means host-provided assemblies always win (SharpFM.Plugin, logging, etc.)
        // and only plugin-specific assemblies load from the plugin's directory.
        context.Resolving += (ctx, name) =>
        {
            var candidate = Path.Combine(pluginDir, name.Name + ".dll");
            if (!File.Exists(candidate)) return null;
            _logger.LogDebug("Plugin dependency resolved: {Assembly} -> {Path}", name.Name, candidate);
            return ctx.LoadFromAssemblyPath(candidate);
        };

        var assembly = context.LoadFromAssemblyPath(fullPath);

        var candidateTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in candidateTypes)
        {
            if (Activator.CreateInstance(type) is not IPlugin plugin) continue;

            plugin.Initialize(host);
            _configService.Apply(plugin);
            _plugins.Add(plugin);

            _logger.LogInformation("Loaded plugin: {Id} ({DisplayName})", plugin.Id, plugin.DisplayName);
        }
    }
}
