using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpFM.Plugin;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Tests.Services;

public class HostUpdateCheckTests
{
    private static HostUpdateCheck CheckerWith(HttpResponseMessage response, string runningVersion = "1.0.0")
    {
        var handler = new FakeHandler(_ => Task.FromResult(response));
        return new HostUpdateCheck(new HttpClient(handler), runningVersion);
    }

    private static HostUpdateCheck CheckerThrowing(Exception ex, string runningVersion = "1.0.0")
    {
        var handler = new FakeHandler(_ => throw ex);
        return new HostUpdateCheck(new HttpClient(handler), runningVersion);
    }

    private static HttpResponseMessage ReleaseResponse(string tagName, string url = "https://example.invalid/r", string? body = null)
    {
        var json = $$"""
            {"tag_name":"{{tagName}}","html_url":"{{url}}","body":{{(body is null ? "null" : "\"" + body + "\"")}}}
            """;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    [Fact]
    public async Task ReturnsUpdate_WhenLatestIsNewer()
    {
        var checker = CheckerWith(ReleaseResponse("v2.1.0"), runningVersion: "2.0.0");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.1.0", result.LatestVersion);
        Assert.Equal(new Uri("https://example.invalid/r"), result.ReleaseUrl);
    }

    [Fact]
    public async Task ReturnsNoUpdate_WhenLatestEqualsRunning()
    {
        var checker = CheckerWith(ReleaseResponse("v2.0.0"), runningVersion: "2.0.0");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task ReturnsNoUpdate_WhenLatestIsOlder()
    {
        var checker = CheckerWith(ReleaseResponse("v1.9.0"), runningVersion: "2.0.0");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task ReturnsNoUpdate_OnHttp404()
    {
        var checker = CheckerWith(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
        Assert.Null(result.LatestVersion);
    }

    [Fact]
    public async Task ReturnsNoUpdate_OnNetworkException()
    {
        var checker = CheckerThrowing(new HttpRequestException("no network"));

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task ReturnsNoUpdate_OnMalformedJson()
    {
        var bad = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", System.Text.Encoding.UTF8, "application/json"),
        };
        var checker = CheckerWith(bad);

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var checker = CheckerThrowing(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => checker.CheckForUpdatesAsync(cts.Token));
    }

    [Fact]
    public async Task StripsCommitSuffix_FromRunningVersion()
    {
        // MinVer's AssemblyInformationalVersion looks like "2.0.0-beta.0+abc123".
        // The build-metadata suffix after '+' must be stripped before comparison.
        var checker = CheckerWith(ReleaseResponse("v2.0.0"), runningVersion: "2.0.0+abc123");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task StripsVPrefix_FromReleaseTag()
    {
        var checker = CheckerWith(ReleaseResponse("v1.0.0"), runningVersion: "1.0.0");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.False(result.UpdateAvailable);
        Assert.Equal("1.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CarriesReleaseNotes_WhenBodyPresent()
    {
        var checker = CheckerWith(ReleaseResponse("v2.0.0", body: "shiny new release"), runningVersion: "1.0.0");

        var result = await checker.CheckForUpdatesAsync(CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("shiny new release", result.Notes);
    }

    [Fact]
    public void ImplementsIUpdateCheckable()
    {
        Assert.True(typeof(IUpdateCheckable).IsAssignableFrom(typeof(HostUpdateCheck)));
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _send;
        public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) => _send = send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _send(request);
    }
}
