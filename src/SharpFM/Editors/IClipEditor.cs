using System;

namespace SharpFM.Editors;

/// <summary>
/// Abstraction for clip-type-specific editing. Each clip type provides an implementation
/// that handles change detection, XML serialization, and reverse sync. ClipViewModel holds
/// one IClipEditor and delegates all sync operations to it — no clip-type branching needed.
///
/// Editors follow a save/dirty model:
/// - User edits make the editor dirty (local buffer diverges from model).
/// - Save flushes the local buffer to the model (ToXml returns authoritative XML).
/// - External mutations (FromXml) re-render from the model, clearing dirty state.
/// </summary>
public interface IClipEditor
{
    /// <summary>
    /// Fires when the editor transitions from clean to dirty (user made an edit).
    /// Does NOT fire on every keystroke — only on the first change after a save or load.
    /// </summary>
    event EventHandler? BecameDirty;

    /// <summary>
    /// Fires after a successful <see cref="Save"/>, indicating the model has been updated.
    /// </summary>
    event EventHandler? Saved;

    /// <summary>
    /// Whether the editor has unsaved changes (local buffer differs from model).
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Flush the editor's local buffer to the model. Validates the content first.
    /// On success: <see cref="IsDirty"/> becomes false, <see cref="IsPartial"/> becomes false,
    /// <see cref="Saved"/> fires, and <see cref="ToXml"/> returns the newly saved state.
    /// On failure (invalid content): returns false, <see cref="IsDirty"/> stays true,
    /// <see cref="IsPartial"/> becomes true, <see cref="Saved"/> does NOT fire,
    /// and <see cref="ToXml"/> continues to return the last valid state.
    /// </summary>
    bool Save();

    /// <summary>
    /// Serialize the current model state to XML. Returns the last saved state, not
    /// the live editor buffer. Call <see cref="Save"/> first to flush pending edits.
    /// </summary>
    string ToXml();

    /// <summary>
    /// Load XML into the editor from an external source (plugin, agent, other editor).
    /// Replaces the local buffer and model. Clears dirty state. Preserves cursor position.
    /// </summary>
    void FromXml(string xml);

    /// <summary>
    /// True if the last <see cref="Save"/> produced output from an incomplete or errored parse.
    /// For example, a half-typed script step that can't fully round-trip.
    /// </summary>
    bool IsPartial { get; }
}
