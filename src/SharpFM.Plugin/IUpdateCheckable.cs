using System.Threading;
using System.Threading.Tasks;

namespace SharpFM.Plugin;

/// <summary>
/// Opt-in capability for plugins that have a remote update channel. Implementing
/// this interface is independent of <see cref="IPlugin"/> — a plugin opts in by
/// implementing both. Plugins that ship with the host binary (or otherwise have
/// no separate release cadence) should not implement this; the host will simply
/// display <see cref="IPlugin.Version"/> without a check-for-updates affordance.
/// </summary>
public interface IUpdateCheckable
{
    /// <summary>
    /// Query the plugin's update channel and return what the host should surface
    /// in the About dialog. Implementations choose the channel (public manifest,
    /// licensing endpoint, anonymous GitHub releases for a public repo, etc.) —
    /// the host has no knowledge of any specific plugin's source.
    ///
    /// <para>
    /// Implementations should fail silently on network errors, returning a
    /// result with <see cref="UpdateCheckResult.UpdateAvailable"/> set to
    /// <c>false</c> rather than throwing. A thrown exception is treated by the
    /// host as a check failure and silently suppressed.
    /// </para>
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct);
}
