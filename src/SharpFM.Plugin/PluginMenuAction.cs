using System;

namespace SharpFM.Plugin;

/// <summary>
/// A custom menu action that a plugin registers in the host's Plugins menu.
/// </summary>
/// <param name="Label">Display text for the menu item.</param>
/// <param name="Callback">Action invoked when the menu item is clicked.</param>
/// <param name="Gesture">Optional keyboard gesture string (e.g. "Ctrl+Shift+X").</param>
public record PluginMenuAction(string Label, Action Callback, string? Gesture = null);
