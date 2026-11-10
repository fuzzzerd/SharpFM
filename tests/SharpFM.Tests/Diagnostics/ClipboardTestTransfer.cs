using Avalonia.Input;

namespace SharpFM.Tests.Diagnostics;

internal sealed class ClipboardTestTransfer(params IAsyncDataTransferItem[] items) : IAsyncDataTransfer
{
    public IReadOnlyList<DataFormat> Formats => DiscoverFormats?.Invoke()
        ?? items.SelectMany(i => i.Formats).Distinct().ToArray();
    public IReadOnlyList<IAsyncDataTransferItem> Items => items;
    public Func<IReadOnlyList<DataFormat>>? DiscoverFormats { get; init; }
    public Action? OnDispose { get; init; }
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Disposed = true;
        OnDispose?.Invoke();
    }
}

internal sealed class ClipboardTestItem(
    IReadOnlyList<DataFormat> formats, Func<DataFormat, Task<object?>> retrieve) : IAsyncDataTransferItem
{
    public IReadOnlyList<DataFormat> Formats => formats;
    public Task<object?> TryGetRawAsync(DataFormat format) => retrieve(format);
}
