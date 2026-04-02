// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using Avalonia.Controls;

namespace SharpFM.Plugin;

/// <summary>
/// A plugin that provides a dockable side panel in the SharpFM UI.
/// Inherits common metadata and lifecycle from <see cref="IPlugin"/>.
/// </summary>
public interface IPanelPlugin : IPlugin
{
    /// <summary>
    /// Create the panel control to be hosted in the main window sidebar.
    /// Called once after <see cref="IPlugin.Initialize"/>.
    /// </summary>
    Control CreatePanel();
}
