using System.Collections.Generic;

namespace SharpFM.Models;

/// <summary>
/// Persisted UI session — the set of clips the user had open across the editor
/// tab strip and which one was active. Restored on launch after the clip
/// repository has finished loading.
/// </summary>
/// <param name="OpenTabs">
/// Tabs in their visual order. References that don't resolve against the
/// current clip catalog on restore are silently skipped — they were deleted
/// or renamed between sessions.
/// </param>
/// <param name="ActiveTab">
/// The previously active tab, or <c>null</c> if no tab was active. Skipped
/// silently on restore if it doesn't resolve.
/// </param>
public sealed record SessionState(
    IReadOnlyList<TabRef> OpenTabs,
    TabRef? ActiveTab)
{
    /// <summary>Shared empty state — used when no session file exists yet.</summary>
    public static SessionState Empty { get; } = new([], null);
}

/// <summary>
/// Stable enough handle for a clip across sessions: its folder path plus its
/// name. Mirrors <c>ClipData</c>'s identity. Renames or deletions invalidate
/// the reference — by design, those tabs simply don't restore.
/// </summary>
public sealed record TabRef(
    IReadOnlyList<string> FolderPath,
    string Name);
