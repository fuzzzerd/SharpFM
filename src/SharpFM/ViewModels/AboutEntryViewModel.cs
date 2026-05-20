using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpFM.Plugin;

namespace SharpFM.ViewModels;

/// <summary>
/// One row in the About dialog — the host itself or a loaded plugin. Holds
/// static metadata (display name, version) and, when the underlying component
/// implements <see cref="IUpdateCheckable"/>, drives the on-demand update
/// check whose outcome is surfaced via <see cref="Status"/> and
/// <see cref="UpdateUrl"/>.
/// </summary>
public sealed class AboutEntryViewModel : INotifyPropertyChanged
{
    private readonly IUpdateCheckable? _checker;

    public AboutEntryViewModel(string displayName, string version, IUpdateCheckable? checker)
    {
        DisplayName = displayName;
        Version = version;
        _checker = checker;
    }

    public string DisplayName { get; }
    public string Version { get; }

    /// <summary>
    /// <c>true</c> when the underlying component opted in to update checks by
    /// implementing <see cref="IUpdateCheckable"/>. The view binds a
    /// "Check for updates" affordance to this.
    /// </summary>
    public bool CanCheckForUpdates => _checker is not null;

    private string? _status;
    public string? Status
    {
        get => _status;
        private set { _status = value; NotifyPropertyChanged(); }
    }

    private Uri? _updateUrl;
    public Uri? UpdateUrl
    {
        get => _updateUrl;
        private set { _updateUrl = value; NotifyPropertyChanged(); }
    }

    /// <summary>
    /// Run the underlying update check and reflect the outcome on
    /// <see cref="Status"/> / <see cref="UpdateUrl"/>. Exceptions other than
    /// <see cref="OperationCanceledException"/> are swallowed so a broken
    /// channel can't break the dialog.
    /// </summary>
    public async Task CheckAsync(CancellationToken ct)
    {
        if (_checker is null) return;

        Status = "Checking…";
        UpdateUrl = null;

        try
        {
            var result = await _checker.CheckForUpdatesAsync(ct).ConfigureAwait(true);
            if (result.UpdateAvailable && result.LatestVersion is not null)
            {
                Status = $"Update available: {result.LatestVersion}";
                UpdateUrl = result.ReleaseUrl;
            }
            else
            {
                Status = "Up to date.";
                UpdateUrl = null;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            Status = "Could not check for updates.";
            UpdateUrl = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
