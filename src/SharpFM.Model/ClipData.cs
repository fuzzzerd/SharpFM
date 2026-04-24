using System.Collections.Generic;

namespace SharpFM.Model;

/// <summary>
/// Data transfer object for clip persistence. Used by <see cref="IClipRepository"/>
/// implementations to load and save clips.
/// </summary>
/// <param name="Name">Clip display name (filename without extension for file-based storage).</param>
/// <param name="ClipType">Clipboard format identifier (e.g. "Mac-XMSS").</param>
/// <param name="Xml">Raw XML content of the clip.</param>
public record ClipData(string Name, string ClipType, string Xml)
{
    /// <summary>
    /// Hierarchy the clip lives under, as an ordered list of folder segment
    /// names. Empty = root. Segments are plain strings (no slashes); each
    /// repository implementation decides how to map them to its storage
    /// (subdirectories, record columns, URL path, etc.).
    /// </summary>
    public IReadOnlyList<string> FolderPath { get; init; } = [];
}
