using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using SharpFM.Model;

namespace SharpFM.Services;

public class ClipboardService : IClipboardService
{
    private static readonly Dictionary<string, string> MacFormats = new()
    {
        ["Mac-XMSC"] = "dyn.ah62d4rv4gk8zuxnxkq",
        ["Mac-XMSS"] = "dyn.ah62d4rv4gk8zuxnxnq",
        ["Mac-XMTB"] = "dyn.ah62d4rv4gk8zuxnykk",
        ["Mac-XMFD"] = "dyn.ah62d4rv4gk8zuxngku",
        ["Mac-XML2"] = "dyn.ah62d4rv4gk8zuxnqgk",
        ["Mac-XMFN"] = "dyn.ah62d4rv4gk8zuxngm2",
        ["Mac-XMVL"] = "dyn.ah62d4rv4gk8zuxn0mu"
    };

    private readonly Window? _window;
    private readonly Func<Task<IAsyncDataTransfer?>> _read;
    private readonly Func<IAsyncDataTransfer, Task> _write;
    private readonly bool _isMacOS;

    public ClipboardService(Window window)
        : this(() => GetClipboard(window).TryGetDataAsync(), data => GetClipboard(window).SetDataAsync(data), OperatingSystem.IsMacOS())
    {
        _window = window;
    }

    internal ClipboardService(Func<Task<IAsyncDataTransfer?>> read, Func<IAsyncDataTransfer, Task> write, bool isMacOS)
    {
        _read = read;
        _write = write;
        _isMacOS = isMacOS;
    }

    public async Task<IReadOnlyList<Clip>> ReadFileMakerAsync()
    {
        var clips = new List<Clip>();
        using var transfer = await _read();
        if (transfer is null) return clips;
        foreach (var item in transfer.Items)
        {
            foreach (var format in item.Formats)
            {
                var logical = MacFormats.FirstOrDefault(pair => pair.Value == format.Identifier).Key ?? format.Identifier;
                if (!logical.StartsWith("Mac-", StringComparison.OrdinalIgnoreCase)) continue;
                // Use the exposed descriptor: macOS captures contain XML strings, not wire bytes.
                var clip = await item.TryGetRawAsync(format) switch
                {
                    string xml => Clip.FromXml("new-clip", logical, xml),
                    byte[] bytes => Clip.FromWireBytes("new-clip", logical, bytes),
                    _ => null
                };
                if (clip is not null) clips.Add(clip);
            }
        }
        return clips;
    }

    public async Task WriteFileMakerAsync(Clip clip)
    {
        var transfer = new DataTransfer();
        if (_isMacOS)
        {
            if (!MacFormats.TryGetValue(clip.FormatId, out var format))
                throw new NotSupportedException("No macOS clipboard mapping exists for this clip type.");
            transfer.Add(DataTransferItem.Create(DataFormat.CreateStringPlatformFormat(format), clip.Xml));
        }
        else
            transfer.Add(DataTransferItem.Create(DataFormat.CreateBytesPlatformFormat(clip.FormatId), clip.WireBytes));
        await _write(transfer);
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

    private IClipboard GetClipboard() => GetClipboard(_window);

    private static IClipboard GetClipboard(Window? window) =>
        window?.Clipboard ?? throw new InvalidOperationException("Clipboard is not available.");
}
