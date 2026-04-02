// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System;

namespace SharpFM.Plugin;

/// <summary>
/// A custom menu action that a plugin registers in the host's Plugins menu.
/// </summary>
/// <param name="Label">Display text for the menu item.</param>
/// <param name="Callback">Action invoked when the menu item is clicked.</param>
/// <param name="Gesture">Optional keyboard gesture string (e.g. "Ctrl+Shift+X").</param>
public record PluginMenuAction(string Label, Action Callback, string? Gesture = null);
