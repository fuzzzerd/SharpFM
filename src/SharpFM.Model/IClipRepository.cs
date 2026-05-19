using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpFM.Model;

/// <summary>
/// Abstraction for clip storage. The built-in file system storage and
/// plugin-provided backends both implement this interface.
/// </summary>
public interface IClipRepository
{
    /// <summary>
    /// Human-readable name for this storage backend (e.g. "Local Files", "Cloud API").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Display string for the current storage location (e.g., folder path, URL).
    /// </summary>
    string CurrentLocation { get; }

    /// <summary>
    /// Whether this provider supports browsing/switching locations
    /// (e.g., picking a different folder, connecting to a different server).
    /// </summary>
    bool SupportsLocationPicker { get; }

    /// <summary>
    /// Load all clips from the storage backend.
    /// </summary>
    Task<IReadOnlyList<ClipData>> LoadClipsAsync();

    /// <summary>
    /// Save all clips to the storage backend. The implementation should handle
    /// creates, updates, and deletes (i.e., clips not in the list should be removed).
    /// </summary>
    Task SaveClipsAsync(IReadOnlyList<ClipData> clips);

    /// <summary>
    /// Load folder metadata records — one per materialized folder (including
    /// empty ones). Default implementation returns an empty list for backends
    /// that don't model folders independently of clip paths.
    /// </summary>
    Task<IReadOnlyList<FolderData>> LoadFoldersAsync() =>
        Task.FromResult<IReadOnlyList<FolderData>>([]);

    /// <summary>
    /// Save folder metadata. The implementation should handle creates,
    /// updates, and deletes (folders not in the list should be removed).
    /// Default implementation is a no-op for backends that infer folders from
    /// clip paths only.
    /// </summary>
    Task SaveFoldersAsync(IReadOnlyList<FolderData> folders) => Task.CompletedTask;

    /// <summary>
    /// Open a location picker and switch to the selected location.
    /// Only called if <see cref="SupportsLocationPicker"/> is true.
    /// Returns the display string for the new location, or null if cancelled.
    /// </summary>
    Task<string?> PickLocationAsync();
}
