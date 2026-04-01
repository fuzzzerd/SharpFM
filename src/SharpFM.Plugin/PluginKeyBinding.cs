// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System;

namespace SharpFM.Plugin;

/// <summary>
/// A keyboard shortcut that a plugin wants registered in the host window.
/// </summary>
/// <param name="Gesture">Key gesture string (e.g. "Ctrl+Shift+X"). Uses Avalonia gesture format.</param>
/// <param name="Description">Human-readable description shown in menus.</param>
/// <param name="Callback">Action invoked when the shortcut is triggered.</param>
public record PluginKeyBinding(string Gesture, string Description, Action Callback);
