using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Plugin;
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
    public void NewScriptCommand_AddsScriptClip()
    {
        var vm = CreateVm();
        var initialCount = vm.FileMakerClips.Count;
        vm.NewScriptCommand();
        Assert.Equal(initialCount + 1, vm.FileMakerClips.Count);
        Assert.True(vm.SelectedClip?.IsScriptClip);
        Assert.Contains("Created new script", vm.StatusMessage);
    }

    [Fact]
    public void NewTableCommand_AddsTableClip()
    {
        var vm = CreateVm();
        var initialCount = vm.FileMakerClips.Count;
        vm.NewTableCommand();
        Assert.Equal(initialCount + 1, vm.FileMakerClips.Count);
        Assert.True(vm.SelectedClip?.IsTableClip);
        Assert.Contains("Created new table", vm.StatusMessage);
    }

    [Fact]
    public void NewTableCommand_TableEditorIsUsable()
    {
        var vm = CreateVm();
        vm.NewTableCommand();
        var clip = vm.SelectedClip!;

        // TableEditor should lazy-create from the starter XML
        var editor = clip.TableEditor;
        Assert.NotNull(editor);
        Assert.Equal("NewTable", editor!.TableName);

        // AddField command should work
        Assert.True(editor.AddFieldCommand.CanExecute(null));
        editor.AddField();
        Assert.Single(editor.Fields);
        Assert.Equal("NewField", editor.Fields[0].Name);
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
    public void DeleteSelectedClip_RemovesClipFromCollection()
    {
        var vm = CreateVm();
        vm.NewScriptCommand();
        var clip = vm.SelectedClip;
        Assert.NotNull(clip);

        vm.DeleteSelectedClip();

        Assert.DoesNotContain(clip, vm.FileMakerClips);
        Assert.Null(vm.SelectedClip);
        Assert.Contains("Deleted clip", vm.StatusMessage);
    }

    [Fact]
    public void DeleteSelectedClip_NoSelection_ShowsStatus()
    {
        var vm = CreateVm();
        vm.SelectedClip = null;

        vm.DeleteSelectedClip();

        Assert.Equal("No clip selected", vm.StatusMessage);
    }

    [Fact]
    public void DeleteSelectedClip_RemovesFromFilteredClips()
    {
        var vm = CreateVm();
        vm.NewScriptCommand();
        var clip = vm.SelectedClip!;
        Assert.Contains(clip, vm.FilteredClips);

        vm.DeleteSelectedClip();

        Assert.DoesNotContain(clip, vm.FilteredClips);
    }

    [Fact]
    public void SearchText_FiltersClips()
    {
        var vm = CreateVm();
        vm.NewScriptCommand(); // adds a clip named "New Script"
        vm.SearchText = "zzz_nonexistent";
        Assert.Empty(vm.FilteredClips);
        vm.SearchText = "";
        Assert.NotEmpty(vm.FilteredClips);
    }

    [Fact]
    public void PanelPlugins_DefaultsToEmpty()
    {
        var vm = CreateVm();
        Assert.Empty(vm.PanelPlugins);
    }

    [Fact]
    public void PanelPlugins_CanBeSet()
    {
        var vm = CreateVm();
        var plugin = new StubPanelPlugin();
        vm.PanelPlugins = [plugin];
        Assert.Single(vm.PanelPlugins);
    }

    [Fact]
    public void TogglePluginPanel_ActivatesPlugin()
    {
        var vm = CreateVm();
        var plugin = new StubPanelPlugin();
        vm.PanelPlugins = [plugin];

        vm.TogglePluginPanel(plugin);

        Assert.True(vm.IsPluginPanelVisible);
        Assert.Same(plugin, vm.ActivePlugin);
        Assert.NotNull(vm.PluginPanelControl);
    }

    [Fact]
    public void TogglePluginPanel_DeactivatesSamePlugin()
    {
        var vm = CreateVm();
        var plugin = new StubPanelPlugin();
        vm.PanelPlugins = [plugin];

        vm.TogglePluginPanel(plugin);
        vm.TogglePluginPanel(plugin);

        Assert.False(vm.IsPluginPanelVisible);
        Assert.Null(vm.ActivePlugin);
    }

    [Fact]
    public void TogglePluginPanel_SwitchesPlugins()
    {
        var vm = CreateVm();
        var plugin1 = new StubPanelPlugin { Id = "p1" };
        var plugin2 = new StubPanelPlugin { Id = "p2" };
        vm.PanelPlugins = [plugin1, plugin2];

        vm.TogglePluginPanel(plugin1);
        Assert.Same(plugin1, vm.ActivePlugin);

        vm.TogglePluginPanel(plugin2);
        Assert.Same(plugin2, vm.ActivePlugin);
    }

    [Fact]
    public void PluginWithKeyBindings_ExposesBindings()
    {
        bool called = false;
        var plugin = new StubPanelPlugin();
        plugin.TestKeyBindings = [new PluginKeyBinding("Ctrl+Shift+X", "Test", () => called = true)];

        Assert.Single(plugin.KeyBindings);
        plugin.KeyBindings[0].Callback();
        Assert.True(called);
    }

    private class StubPanelPlugin : IPanelPlugin
    {
        public string Id { get; set; } = "stub";
        public string DisplayName => "Stub Plugin";
        public string Version => "1.0.0-test";
        public IReadOnlyList<PluginKeyBinding> TestKeyBindings { get; set; } = [];
        public IReadOnlyList<PluginKeyBinding> KeyBindings => TestKeyBindings;
        public IReadOnlyList<PluginMenuAction> MenuActions => [];
        public Control CreatePanel() => new TextBlock { Text = "stub" };
        public void Initialize(IPluginHost host) { }
        public void Dispose() { }
    }
}
