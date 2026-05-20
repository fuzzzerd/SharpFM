using System;
using System.Collections.Generic;

namespace SharpFM.Models;

/// <summary>
/// Payload for <see cref="ClipRepository.FolderPathSanitized"/>. Reports the
/// original folder path, the path that was actually used, and the segments
/// that were dropped during sanitization.
/// </summary>
public sealed class FolderPathSanitizedEventArgs : EventArgs
{
    public IReadOnlyList<string> Original { get; }
    public IReadOnlyList<string> Sanitized { get; }
    public IReadOnlyList<string> DroppedSegments { get; }

    public FolderPathSanitizedEventArgs(
        IReadOnlyList<string> original,
        IReadOnlyList<string> sanitized,
        IReadOnlyList<string> droppedSegments)
    {
        Original = original;
        Sanitized = sanitized;
        DroppedSegments = droppedSegments;
    }
}
