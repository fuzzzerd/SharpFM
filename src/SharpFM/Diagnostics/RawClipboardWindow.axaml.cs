using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace SharpFM.Diagnostics;

[ExcludeFromCodeCoverage]
public partial class RawClipboardWindow : Window
{
    private readonly Button _captureButton;
    private readonly Button _saveButton;
    private readonly Button _closeButton;
    private readonly TextBox _version;
    private readonly TextBox _description;
    private readonly TextBox _captureSummary;
    private readonly TextBox _interpretationSummary;
    private readonly ListBox _formatList;
    private readonly TextBlock _status;
    private ClipboardSnapshot? _snapshot;
    private bool _busy;

    public RawClipboardWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _captureButton = this.FindControl<Button>("captureButton")!;
        _saveButton = this.FindControl<Button>("saveButton")!;
        _closeButton = this.FindControl<Button>("closeButton")!;
        _version = this.FindControl<TextBox>("fileMakerVersion")!;
        _description = this.FindControl<TextBox>("captureDescription")!;
        _captureSummary = this.FindControl<TextBox>("captureSummary")!;
        _interpretationSummary = this.FindControl<TextBox>("interpretationSummary")!;
        _formatList = this.FindControl<ListBox>("formatList")!;
        _status = this.FindControl<TextBlock>("statusLabel")!;

        _captureButton.Click += async (_, _) => await CaptureAsync();
        _saveButton.Click += async (_, _) => await SaveAsync();
        _closeButton.Click += (_, _) => Close();
        Closing += (_, e) => e.Cancel = _busy;
        _formatList.SelectionChanged += (_, _) =>
        {
            var index = _formatList.SelectedIndex;
            _interpretationSummary.Text = _snapshot is not null && index >= 0 && index < _snapshot.Entries.Count
                ? ClipboardSnapshotSummary.Describe(_snapshot.Entries[index]) : "No payload selected.";
        };
    }

    private async Task CaptureAsync()
    {
        if (_busy) return;
        SetBusy(true);
        _status.Text = "Capturing all formats exposed by Avalonia…";
        try
        {
            var capture = new ClipboardSnapshotCapture(async () =>
                Clipboard is { } clipboard ? await clipboard.TryGetDataAsync() : null);
            _snapshot = await capture.CaptureAsync(ClipboardCaptureEnvironment.Current(), _version.Text, _description.Text);
            _captureSummary.Text = ClipboardSnapshotSummary.Describe(_snapshot);
            _formatList.ItemsSource = _snapshot.Entries.Select(ClipboardSnapshotSummary.Label).ToArray();
            _formatList.SelectedIndex = _snapshot.Entries.Count > 0 ? 0 : -1;
            _interpretationSummary.Text = _snapshot.Entries.Count > 0
                ? ClipboardSnapshotSummary.Describe(_snapshot.Entries[0]) : "No payloads retrieved. The snapshot still contains discovery/error information.";
            _status.Text = _snapshot.IsPartial
                ? "Partial capture retained. Save the snapshot to share successful payloads and failure details."
                : "Capture retained in memory. Save Snapshot exports this capture, even if the clipboard changes.";
        }
        catch (Exception ex)
        {
            _status.Text = $"Capture failed: {ClipboardDiagnosticError.Describe(ex)}";
        }
        finally { SetBusy(false); }
    }

    private async Task SaveAsync()
    {
        if (_busy || _snapshot is null) return;
        SetBusy(true);
        try
        {
            using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save FileMaker clipboard snapshot (contains full clipboard data)",
                SuggestedFileName = $"sharpfm-clipboard-{_snapshot.CapturedAtUtc:yyyyMMdd-HHmmss}.zip",
                DefaultExtension = "zip",
                FileTypeChoices = [new FilePickerFileType("Clipboard snapshot ZIP") { Patterns = ["*.zip"] }],
                ShowOverwritePrompt = true
            });
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null)
            {
                _status.Text = "Choose a local file destination so a completed snapshot can be saved safely.";
                return;
            }
            _status.Text = "Saving snapshot…";
            var snapshot = _snapshot;
            await Task.Run(() => ClipboardSnapshotExporter.SaveAsync(snapshot, path));
            _status.Text = "Snapshot saved. It contains full clipboard data; only attach it to an issue if safe to share.";
        }
        catch (Exception ex)
        {
            _status.Text = $"Save failed; capture retained for retry: {ClipboardDiagnosticError.Describe(ex)}";
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _captureButton.IsEnabled = !busy;
        _saveButton.IsEnabled = !busy && _snapshot is not null;
        _closeButton.IsEnabled = !busy;
        _version.IsEnabled = !busy;
        _description.IsEnabled = !busy;
    }
}
