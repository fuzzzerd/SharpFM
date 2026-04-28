using System;
using SharpFM.Model.Parsing;

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
    /// Snapshot the editor's live domain model. The editor owns this state
    /// (it produced it from display-text edits), so callers can trust the
    /// returned model reflects the same content <see cref="ToXml"/> would
    /// emit — no re-parse required. Returned model is "as good as" the
    /// editor knows; structural fidelity is the editor's responsibility.
    /// </summary>
    ClipModel GetModel();

    /// <summary>
    /// True if the last <see cref="ToXml"/> produced output from an incomplete or errored parse.
    /// For example, a half-typed script step that can't fully round-trip.
    /// </summary>
    bool IsPartial { get; }
}
