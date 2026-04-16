namespace SharpFM.Model;

/// <summary>
/// Data transfer object for clip persistence. Used by <see cref="IClipRepository"/>
/// implementations to load and save clips.
/// </summary>
/// <param name="Name">Clip display name (filename without extension for file-based storage).</param>
/// <param name="ClipType">Clipboard format identifier (e.g. "Mac-XMSS").</param>
/// <param name="Xml">Raw XML content of the clip.</param>
public record ClipData(string Name, string ClipType, string Xml);
