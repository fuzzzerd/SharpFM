// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

namespace SharpFM.Plugin;

/// <summary>
/// Data transfer object for clip persistence. Used by <see cref="IClipRepository"/>
/// implementations to load and save clips.
/// </summary>
/// <param name="Name">Clip display name (filename without extension for file-based storage).</param>
/// <param name="ClipType">Clipboard format identifier (e.g. "Mac-XMSS").</param>
/// <param name="Xml">Raw XML content of the clip.</param>
public record ClipData(string Name, string ClipType, string Xml);
