// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System;
using System.Collections.Generic;

namespace SharpFM.Plugin;

/// <summary>
/// Services provided by the SharpFM host application to plugins.
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// The currently selected clip, or null if nothing is selected.
    /// </summary>
    ClipInfo? SelectedClip { get; }

    /// <summary>
    /// Raised when the selected clip changes (user selects a different clip in the list).
    /// </summary>
    event EventHandler<ClipInfo?> SelectedClipChanged;

    /// <summary>
    /// Replace the XML content of the currently selected clip.
    /// The host syncs the new XML back to the structured editor automatically.
    /// </summary>
    /// <param name="xml">The new XML content.</param>
    /// <param name="originPluginId">The Id of the plugin making the change,
    /// used for origin tagging so the plugin can skip its own updates.</param>
    void UpdateSelectedClipXml(string xml, string originPluginId);

    /// <summary>
    /// Sync the current editor state to XML and return a fresh snapshot.
    /// Call this before reading <see cref="SelectedClip"/> if you need up-to-date XML
    /// that reflects any in-progress edits in the structured editors.
    /// </summary>
    ClipInfo? RefreshSelectedClip();

    /// <summary>
    /// Raised when clip content changes — either from a user edit in the structured editor
    /// or from a plugin pushing XML. The <see cref="ClipContentChangedArgs.Origin"/> field
    /// indicates who caused the change ("editor" for user edits, or a plugin Id).
    /// Debounced for editor edits; immediate for plugin pushes.
    /// </summary>
    event EventHandler<ClipContentChangedArgs> ClipContentChanged;

    /// <summary>
    /// All clips currently loaded in the application.
    /// Useful for event plugins that need to operate across the full clip set.
    /// </summary>
    IReadOnlyList<ClipInfo> AllClips { get; }

    /// <summary>
    /// Raised when the clip collection changes (clips added, removed, or reloaded).
    /// </summary>
    event EventHandler? ClipCollectionChanged;

    /// <summary>
    /// Request the host to show a status message in the status bar.
    /// Plugins should use this for user-visible feedback rather than
    /// implementing their own notification UI.
    /// </summary>
    void ShowStatus(string message);
}
