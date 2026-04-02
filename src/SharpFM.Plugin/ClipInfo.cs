// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

namespace SharpFM.Plugin;

/// <summary>
/// Read-only snapshot of a clip's metadata and content, provided to plugins by the host.
/// </summary>
public record ClipInfo(string Name, string ClipType, string Xml);
