using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using SharpFM.Plugin;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class AboutEntryViewModelTests
{
    [Fact]
    public void CanCheckForUpdates_IsFalse_WhenNoCheckerProvided()
    {
        var entry = new AboutEntryViewModel("Bundled Plugin", "1.0.0", checker: null);

        Assert.False(entry.CanCheckForUpdates);
    }

    [Fact]
    public void CanCheckForUpdates_IsTrue_WhenCheckerProvided()
    {
        var entry = new AboutEntryViewModel("Host", "1.0.0", new StubChecker(new UpdateCheckResult(false, null, null, null)));

        Assert.True(entry.CanCheckForUpdates);
    }

    [Fact]
    public async Task CheckAsync_SetsStatus_WhenUpdateAvailable()
    {
        var entry = new AboutEntryViewModel("Host", "1.0.0",
            new StubChecker(new UpdateCheckResult(true, "2.0.0", new Uri("https://example.invalid/r"), "release notes")));

        await entry.CheckAsync(CancellationToken.None);

        Assert.Contains("2.0.0", entry.Status);
        Assert.Equal(new Uri("https://example.invalid/r"), entry.UpdateUrl);
    }

    [Fact]
    public async Task CheckAsync_SetsStatus_WhenUpToDate()
    {
        var entry = new AboutEntryViewModel("Host", "2.0.0",
            new StubChecker(new UpdateCheckResult(false, "2.0.0", null, null)));

        await entry.CheckAsync(CancellationToken.None);

        Assert.Contains("up to date", entry.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Null(entry.UpdateUrl);
    }

    [Fact]
    public async Task CheckAsync_FailsSilently_OnException()
    {
        var entry = new AboutEntryViewModel("Host", "1.0.0", new ThrowingChecker());

        await entry.CheckAsync(CancellationToken.None);

        Assert.NotNull(entry.Status);
        Assert.DoesNotContain("Exception", entry.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckAsync_RaisesPropertyChanged_ForStatus()
    {
        var entry = new AboutEntryViewModel("Host", "1.0.0",
            new StubChecker(new UpdateCheckResult(false, null, null, null)));

        var changed = false;
        ((INotifyPropertyChanged)entry).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AboutEntryViewModel.Status)) changed = true;
        };

        await entry.CheckAsync(CancellationToken.None);

        Assert.True(changed);
    }

    [Fact]
    public async Task CheckAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var entry = new AboutEntryViewModel("Host", "1.0.0", new CancellableChecker());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => entry.CheckAsync(cts.Token));
    }

    private sealed class StubChecker(UpdateCheckResult result) : IUpdateCheckable
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct) => Task.FromResult(result);
    }

    private sealed class ThrowingChecker : IUpdateCheckable
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class CancellableChecker : IUpdateCheckable
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new UpdateCheckResult(false, null, null, null));
        }
    }
}
