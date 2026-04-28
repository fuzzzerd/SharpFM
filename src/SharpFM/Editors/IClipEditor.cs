using System;

namespace SharpFM.Editors;

/// <summary>
/// Abstraction for clip-type-specific editing. Each clip type provides an implementation
/// that handles change detection, XML serialization, and reverse sync. ClipViewModel holds
/// one IClipEditor and delegates all sync operations to it — no clip-type branching needed.
/// </summary>
public interface IClipEditor
{
    /// <summary>
    /// Fires when the user edits content in the structured editor.
    /// Implementations should debounce this (e.g. 500ms) to avoid excessive events.
    /// </summary>
    event EventHandler? ContentChanged;

    /// <summary>
    /// Serialize the current editor state to XML.
    /// </summary>
    string ToXml();

    /// <summary>
    /// True if the last <see cref="ToXml"/> produced output from an incomplete or errored parse.
    /// For example, a half-typed script step that can't fully round-trip.
    /// </summary>
    bool IsPartial { get; }
}
