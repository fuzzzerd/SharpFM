// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace SharpFM.Plugin;

/// <summary>
/// A plugin that provides a dockable side panel in the SharpFM UI.
/// </summary>
public interface IPanelPlugin : IDisposable
{
    /// <summary>
    /// Unique identifier for this plugin (e.g. "clip-inspector", "ai-assistant").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in the View menu (e.g. "Clip Inspector").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Create the panel control to be hosted in the main window sidebar.
    /// Called once after <see cref="Initialize"/>.
    /// </summary>
    Control CreatePanel();

    /// <summary>
    /// Initialize the plugin with access to host services.
    /// Called once at startup before <see cref="CreatePanel"/>.
    /// </summary>
    void Initialize(IPluginHost host);

    /// <summary>
    /// Keyboard shortcuts this plugin wants registered in the host window.
    /// The host registers these when the plugin is loaded. Return empty for no shortcuts.
    /// </summary>
    IReadOnlyList<PluginKeyBinding> KeyBindings { get; }
}
