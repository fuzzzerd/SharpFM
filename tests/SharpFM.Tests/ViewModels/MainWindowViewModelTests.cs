using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Services;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class MockClipboardService : IClipboardService
{
    public string? LastText { get; private set; }
    public string? LastFormat { get; private set; }
    public byte[]? LastData { get; private set; }
    public Dictionary<string, object> ClipboardData { get; } = new();

    public Task SetTextAsync(string text) { LastText = text; return Task.CompletedTask; }
    public Task SetDataAsync(string format, byte[] data) { LastFormat = format; LastData = data; return Task.CompletedTask; }
    public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());
    public Task<object?> GetDataAsync(string format) =>
        Task.FromResult(ClipboardData.TryGetValue(format, out var v) ? v : null);
}

public class MockFolderService : IFolderService
{
    public string FolderToReturn { get; set; } = "/tmp/test-clips";
    public Task<string> GetFolderAsync() => Task.FromResult(FolderToReturn);
}

public class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateVm(
        MockClipboardService? clipboard = null,
        MockFolderService? folderService = null)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<MainWindowViewModel>();
        return new MainWindowViewModel(
            logger,
            clipboard ?? new MockClipboardService(),
            folderService ?? new MockFolderService());
    }

    [Fact]
    public void NewEmptyItem_AddsClip()
    {
        var vm = CreateVm();
        var initialCount = vm.FileMakerClips.Count;
        vm.NewEmptyItem();
        Assert.Equal(initialCount + 1, vm.FileMakerClips.Count);
        Assert.Contains("Created new clip", vm.StatusMessage);
    }

    [Fact]
    public async Task CopyAsClass_NoSelection_ShowsStatus()
    {
        var vm = CreateVm();
        vm.SelectedClip = null;
        await vm.CopyAsClass();
        Assert.Equal("No clip selected", vm.StatusMessage);
    }

    [Fact]
    public async Task CopySelectedToClip_NoSelection_ShowsStatus()
    {
        var vm = CreateVm();
        vm.SelectedClip = null;
        await vm.CopySelectedToClip();
        Assert.Equal("No clip selected", vm.StatusMessage);
    }

    [Fact]
    public async Task PasteFileMakerClipData_NoFormats_ShowsStatus()
    {
        var clipboard = new MockClipboardService();
        var vm = CreateVm(clipboard);
        await vm.PasteFileMakerClipData();
        Assert.Contains("No FileMaker clips found", vm.StatusMessage);
    }

    [Fact]
    public void StatusMessage_NotifiesPropertyChanged()
    {
        var vm = CreateVm();
        string? changedProperty = null;
        vm.PropertyChanged += (_, args) => changedProperty = args.PropertyName;
        vm.StatusMessage = "test";
        Assert.Equal("StatusMessage", changedProperty);
    }

    [Fact]
    public void SearchText_FiltersClips()
    {
        var vm = CreateVm();
        vm.NewEmptyItem(); // adds a clip named "New"
        vm.SearchText = "zzz_nonexistent";
        Assert.Empty(vm.FilteredClips);
        vm.SearchText = "";
        Assert.NotEmpty(vm.FilteredClips);
    }
}
