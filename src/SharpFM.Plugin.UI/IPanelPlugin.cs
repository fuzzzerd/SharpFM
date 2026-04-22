using Avalonia.Controls;

namespace SharpFM.Plugin.UI;

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
