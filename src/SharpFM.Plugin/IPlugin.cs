using System;
using System.Collections.Generic;

namespace SharpFM.Plugin;

/// <summary>
/// Base interface for all SharpFM plugins. Provides common metadata and lifecycle.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Unique identifier for this plugin (e.g. "clip-inspector", "ai-assistant").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in the Plugins menu (e.g. "Clip Inspector").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Short description of what this plugin does, shown in the Plugin Manager UI.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin version string (e.g. "2.0.0-beta.0"). Shown in the Plugin Manager UI.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Initialize the plugin with access to host services.
    /// Called once at startup before any other interaction.
    /// </summary>
    void Initialize(IPluginHost host);

    /// <summary>
    /// Keyboard shortcuts this plugin wants registered in the host window.
    /// The host registers these when the plugin is loaded. Return empty for no shortcuts.
    /// </summary>
    IReadOnlyList<PluginKeyBinding> KeyBindings { get; }

    /// <summary>
    /// Custom menu actions shown under this plugin's entry in the Plugins menu.
    /// If empty, the plugin shows as a simple toggle item. If non-empty, it shows
    /// as a submenu with "Toggle Panel" plus these custom actions.
    /// </summary>
    IReadOnlyList<PluginMenuAction> MenuActions { get; }
}
