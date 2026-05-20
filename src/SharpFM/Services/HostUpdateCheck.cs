using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SharpFM.Plugin;

namespace SharpFM.Services;

/// <summary>
/// Host-side update check against the public SharpFM releases feed on GitHub.
/// Implements <see cref="IUpdateCheckable"/> so the About dialog can iterate
/// host + plugins uniformly. Anonymous request — no auth header, no telemetry.
/// </summary>
public sealed class HostUpdateCheck : IUpdateCheckable
{
    /// <summary>Public releases endpoint for the host app. Anonymous, rate-limited at 60/hour/IP.</summary>
    public const string ReleasesEndpoint = "https://api.github.com/repos/fuzzzerd/SharpFM/releases/latest";

    private readonly HttpClient _http;
    private readonly string _runningVersion;

    public HostUpdateCheck(HttpClient http, string runningVersion)
    {
        _http = http;
        _runningVersion = runningVersion;
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            // GitHub's API rejects requests without a User-Agent.
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("SharpFM-update-check");
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(ReleasesEndpoint, ct).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return NoUpdate;
            }

            var release = await response.Content.ReadFromJsonAsync<ReleasePayload>(ct).ConfigureAwait(false);
            if (release is null || string.IsNullOrEmpty(release.TagName))
            {
                return NoUpdate;
            }

            var latest = StripVPrefix(release.TagName);
            var running = StripBuildMetadata(_runningVersion);

            if (!IsNewer(latest, running))
            {
                return new UpdateCheckResult(false, latest, ParseUri(release.HtmlUrl), release.Body);
            }

            return new UpdateCheckResult(true, latest, ParseUri(release.HtmlUrl), release.Body);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return NoUpdate;
        }
    }

    private static readonly UpdateCheckResult NoUpdate = new(false, null, null, null);

    private static string StripVPrefix(string tag) =>
        tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;

    private static string StripBuildMetadata(string version)
    {
        var plus = version.IndexOf('+');
        return plus < 0 ? version : version[..plus];
    }

    private static Uri? ParseUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;

    /// <summary>
    /// SemVer-aware "is latest strictly greater than running" comparison for the
    /// MAJOR.MINOR.PATCH core. Pre-release qualifiers are ignored — the
    /// <c>/releases/latest</c> endpoint already excludes them.
    /// </summary>
    internal static bool IsNewer(string latest, string running)
    {
        var l = CoreParts(latest);
        var r = CoreParts(running);
        for (var i = 0; i < 3; i++)
        {
            if (l[i] > r[i]) return true;
            if (l[i] < r[i]) return false;
        }
        return false;
    }

    private static int[] CoreParts(string version)
    {
        // Drop any pre-release qualifier ("-beta.3") before splitting.
        var dash = version.IndexOf('-');
        var core = dash < 0 ? version : version[..dash];
        var parts = core.Split('.', 4);
        var result = new int[3];
        for (var i = 0; i < 3 && i < parts.Length; i++)
        {
            int.TryParse(parts[i], out result[i]);
        }
        return result;
    }

    private sealed record ReleasePayload(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("body")] string? Body);
}
