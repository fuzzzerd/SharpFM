using SharpFM.Model;

namespace SharpFM.Plugin;

/// <summary>
/// A plugin that provides an alternative clip storage backend.
/// The host registers this as an available persistence provider
/// that users can switch to via the UI.
/// </summary>
public interface IPersistencePlugin : IPlugin
{
    /// <summary>
    /// Create and return the repository instance for this storage backend.
    /// Called when the user selects this provider.
    /// </summary>
    IClipRepository CreateRepository();
}
