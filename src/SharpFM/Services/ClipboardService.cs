using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Data;

namespace SharpFM.Services;

public class ClipboardService : IClipboardService
{
    private readonly Window _window;

    public ClipboardService(Window window)
    {
        _window = window;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = _window.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available.");
        await clipboard.SetTextAsync(text);
    }

    public async Task SetDataAsync(string format, byte[] data)
    {
        var clipboard = _window.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available.");
        var dp = new DataPackage();
        dp.SetData(format, data);
        await clipboard.SetDataObjectAsync(dp);
    }

    public async Task<string[]> GetFormatsAsync()
    {
        var clipboard = _window.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available.");
        return await clipboard.GetFormatsAsync();
    }

    public async Task<object?> GetDataAsync(string format)
    {
        var clipboard = _window.Clipboard
            ?? throw new InvalidOperationException("Clipboard is not available.");
        return await clipboard.GetDataAsync(format);
    }
}
