using System;

namespace SharpFM.Plugin;

/// <summary>
/// A keyboard shortcut that a plugin wants registered in the host window.
/// </summary>
/// <param name="Gesture">Key gesture string (e.g. "Ctrl+Shift+X"). Uses Avalonia gesture format.</param>
/// <param name="Description">Human-readable description shown in menus.</param>
/// <param name="Callback">Action invoked when the shortcut is triggered.</param>
public record PluginKeyBinding(string Gesture, string Description, Action Callback);
