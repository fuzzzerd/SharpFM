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
    /// Open a location picker and switch to the selected location.
    /// Only called if <see cref="SupportsLocationPicker"/> is true.
    /// Returns the display string for the new location, or null if cancelled.
    /// </summary>
    Task<string?> PickLocationAsync();
}
