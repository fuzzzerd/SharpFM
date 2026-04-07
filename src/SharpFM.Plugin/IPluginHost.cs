// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SharpFM.Model;
using SharpFM.Model.Schema;
using SharpFM.Model.Scripting;

namespace SharpFM.Plugin;

/// <summary>
/// Services provided by the SharpFM host application to plugins.
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// Create an <see cref="ILogger"/> for the given category name.
    /// Logs are routed through the host application's logging infrastructure.
    /// </summary>
    ILogger CreateLogger(string categoryName);

    /// <summary>
    /// The currently selected clip, or null if nothing is selected.
    /// </summary>
    ClipData? SelectedClip { get; }

    /// <summary>
    /// Raised when the selected clip changes (user selects a different clip in the list).
    /// </summary>
    event EventHandler<ClipData?> SelectedClipChanged;

    /// <summary>
    /// Replace the XML content of the currently selected clip.
    /// The host syncs the new XML back to the structured editor automatically.
    /// </summary>
    void UpdateSelectedClipXml(string xml, string originPluginId);

    /// <summary>
    /// Sync the current editor state to XML and return a fresh snapshot.
    /// Call this before reading <see cref="SelectedClip"/> if you need up-to-date XML
    /// that reflects any in-progress edits in the structured editors.
    /// </summary>
    ClipData? RefreshSelectedClip();

    /// <summary>
    /// Raised when clip content changes — either from a user edit in the structured editor
    /// or from a plugin pushing XML. The <see cref="ClipContentChangedArgs.Origin"/> field
    /// indicates who caused the change ("editor" for user edits, or a plugin Id).
    /// </summary>
    event EventHandler<ClipContentChangedArgs> ClipContentChanged;

    /// <summary>
    /// All clips currently loaded in the application.
    /// </summary>
    IReadOnlyList<ClipData> AllClips { get; }

    /// <summary>
    /// Raised when the clip collection changes (clips added, removed, or reloaded).
    /// </summary>
    event EventHandler? ClipCollectionChanged;

    /// <summary>
    /// Show a status message in the status bar.
    /// </summary>
    void ShowStatus(string message);

    /// <summary>
    /// Get fresh XML for any loaded clip by name.
    /// If the clip is currently selected, syncs the editor state first.
    /// </summary>
    ClipData? GetClip(string clipName);

    /// <summary>
    /// Replace the XML content of any loaded clip by name.
    /// If the clip is currently selected, syncs the change to the editor.
    /// </summary>
    void UpdateClipXml(string clipName, string xml, string originPluginId);

    /// <summary>
    /// Create a new clip and add it to the loaded collection.
    /// </summary>
    void CreateClip(string name, string clipType, string? xml = null);

    /// <summary>
    /// Remove a clip from the loaded collection by name.
    /// </summary>
    bool RemoveClip(string clipName);
}
