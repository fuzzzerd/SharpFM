using System;

namespace SharpFM.Plugin;

/// <summary>
/// Outcome of an <see cref="IUpdateCheckable.CheckForUpdatesAsync"/> call.
/// All three optional fields may be <c>null</c> when
/// <see cref="UpdateAvailable"/> is <c>false</c>.
/// </summary>
/// <param name="UpdateAvailable">
/// <c>true</c> when the channel reports a newer version than what the plugin
/// is currently running.
/// </param>
/// <param name="LatestVersion">
/// Human-readable version string of the latest release (e.g. <c>"2.0.0"</c>).
/// Format is the plugin's choice; the host displays it verbatim.
/// </param>
/// <param name="ReleaseUrl">
/// Where to point the user to obtain the update — typically a release notes
/// or download page. Opened in the system browser on click.
/// </param>
/// <param name="Notes">
/// Optional short summary surfaced alongside the version (e.g. release
/// highlights). The host shows it as plain text; do not include markup.
/// </param>
public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string? LatestVersion,
    Uri? ReleaseUrl,
    string? Notes);
