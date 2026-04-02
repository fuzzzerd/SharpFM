// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

namespace SharpFM.Plugin;

/// <summary>
/// Event arguments for <see cref="IPluginHost.ClipContentChanged"/>.
/// </summary>
/// <param name="Clip">Fresh snapshot of the clip with synced XML.</param>
/// <param name="Origin">"editor" for user edits, or the originating plugin's <see cref="IPanelPlugin.Id"/>.</param>
/// <param name="IsPartial">True if the XML was produced from an incomplete parse (e.g. mid-typing).</param>
public record ClipContentChangedArgs(ClipInfo Clip, string Origin, bool IsPartial);
