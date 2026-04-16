using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using SharpFM.Model;
using SharpFM.Services;

namespace SharpFM.Diagnostics;

[ExcludeFromCodeCoverage]
public partial class RawClipboardWindow : Window
{
    private readonly ClipboardService _clipboard;
    private readonly TextEditor _editor;
    private readonly TextBlock _formatLabel;
    private readonly TextBlock _warningLabel;
    private readonly TextBlock _statusLabel;

    public RawClipboardWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _clipboard = new ClipboardService(this);
        _editor = this.FindControl<TextEditor>("xmlEditor")!;
        _formatLabel = this.FindControl<TextBlock>("formatLabel")!;
        _warningLabel = this.FindControl<TextBlock>("warningLabel")!;
        _statusLabel = this.FindControl<TextBlock>("statusLabel")!;

        this.FindControl<Button>("pasteButton")!.Click += async (_, _) => await OnPaste();
        this.FindControl<Button>("copyButton")!.Click += async (_, _) => await OnCopy();
        this.FindControl<Button>("closeButton")!.Click += (_, _) => Close();
    }

    private async System.Threading.Tasks.Task OnPaste()
    {
        try
        {
            var formats = await _clipboard.GetFormatsAsync();
            var fmFormats = formats
                .Where(f => f.StartsWith("Mac-XM", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToArray();

            if (fmFormats.Length == 0)
            {
                _formatLabel.Text = "(none)";
                _warningLabel.Text = "";
                _editor.Text = "";
                _statusLabel.Text = "No FileMaker clip found on clipboard.";
                return;
            }

            var first = fmFormats[0];
            var data = await _clipboard.GetDataAsync(first);

            if (data is not byte[] bytes)
            {
                _statusLabel.Text = $"Clipboard entry for {first} was not a byte array.";
                return;
            }

            var xml = FileMakerClip.ClipBytesToPrettyXml(bytes.Skip(4));

            _formatLabel.Text = first;
            _editor.Text = xml;
            _warningLabel.Text = fmFormats.Length > 1
                ? $"Multiple FileMaker formats present ({string.Join(", ", fmFormats)}); only the first was rendered."
                : "";
            _statusLabel.Text = $"Pasted {bytes.Length} bytes.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task OnCopy()
    {
        try
        {
            await _clipboard.SetTextAsync(_editor.Text ?? string.Empty);
            _statusLabel.Text = "XML copied to clipboard.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
        }
    }
}
