using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace SharpFM.Services;

[ExcludeFromCodeCoverage]
public class ClipboardService : IClipboardService
{
    private readonly Window _window;

    public ClipboardService(Window window)
    {
        _window = window;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        await clipboard.SetTextAsync(text);
    }

    public async Task SetDataAsync(string format, byte[] data)
    {
        var clipboard = GetClipboard();
        var dataFormat = DataFormat.CreateBytesPlatformFormat(format);
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(dataFormat, data));
        await clipboard.SetDataAsync(transfer);
    }

    public async Task<string[]> GetFormatsAsync()
    {
        var clipboard = GetClipboard();
        var formats = await clipboard.GetDataFormatsAsync();
        return formats.Select(f => f.Identifier).ToArray();
    }

    public async Task<object?> GetDataAsync(string format)
    {
        var clipboard = GetClipboard();
        using var transfer = await clipboard.TryGetDataAsync();
        if (transfer is null) return null;
        var dataFormat = DataFormat.CreateBytesPlatformFormat(format);
        return await transfer.TryGetValueAsync(dataFormat);
    }

    private IClipboard GetClipboard() =>
        _window.Clipboard ?? throw new InvalidOperationException("Clipboard is not available.");
}
