using System.Collections.Generic;

namespace SharpFM.Model;

/// <summary>
/// Data transfer object for folder-metadata persistence. Carries the FileMaker
/// <c>&lt;Group&gt;</c> attributes that have no home on individual
/// <see cref="ClipData"/> entries. An entry exists for every folder the user
/// has materialized — including empty folders that contain no clips.
/// </summary>
/// <param name="Path">
/// Hierarchy this folder lives at, as an ordered list of segment names. The
/// final segment is the folder's own name. Empty paths are not valid.
/// </param>
public sealed record FolderData(IReadOnlyList<string> Path)
{
    /// <summary>
    /// FileMaker <c>Group/@id</c>, preserved when the folder came from a paste.
    /// Null for folders the user created locally; the host assigns an id when
    /// the folder is copied back to FileMaker.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// FileMaker <c>Group/@includeInMenu</c>. Defaults to <c>true</c>, matching
    /// FileMaker's default for new groups.
    /// </summary>
    public bool IncludeInMenu { get; init; } = true;

    /// <summary>
    /// FileMaker <c>Group/@groupCollapsed</c>. Defaults to <c>false</c>; the UI
    /// uses this to pre-collapse a pasted group on load.
    /// </summary>
    public bool GroupCollapsed { get; init; }
}
