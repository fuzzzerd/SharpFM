using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SharpFM.Services;

public class FolderService(Window target)
{
    private readonly Window _target = target;

    public async Task<string> GetFolderAsync()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(_target) ?? throw new ArgumentNullException(nameof(_target), "Window Target.");

        // Start async operation to open the dialog.
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select a Folder",
        });

        var folder = folders.SingleOrDefault();

        return folder?.TryGetLocalPath() ?? throw new ArgumentException("Could not load local path.");
    }
}