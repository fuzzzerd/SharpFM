using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;
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
    public void AllPlugins_DefaultsToEmpty()
    {
        var vm = CreateVm();
        Assert.Empty(vm.AllPlugins);
    }

    [Fact]
    public void PluginUI_TogglePanel_ActivatesPlugin()
    {
        var vm = CreateVm();
        var plugin = new StubPanelPlugin();
        vm.AllPlugins = [plugin];
        var host = new MockPluginHost();
        var uiHost = new PluginUIHost(host);
        vm.PluginUI = uiHost;

        uiHost.TogglePanel(plugin);

        Assert.True(uiHost.IsVisible);
        Assert.Equal(plugin.Id, uiHost.ActivePluginId);
        Assert.NotNull(uiHost.PanelControl);
    }

    [Fact]
    public void PluginUI_TogglePanel_DeactivatesSamePlugin()
    {
        var vm = CreateVm();
        var plugin = new StubPanelPlugin();
        vm.AllPlugins = [plugin];
        var host = new MockPluginHost();
        var uiHost = new PluginUIHost(host);
        vm.PluginUI = uiHost;

        uiHost.TogglePanel(plugin);
        uiHost.TogglePanel(plugin);

        Assert.False(uiHost.IsVisible);
        Assert.Null(uiHost.ActivePluginId);
    }

    [Fact]
    public void PluginUI_TogglePanel_SwitchesPlugins()
    {
        var vm = CreateVm();
        var plugin1 = new StubPanelPlugin { Id = "p1" };
        var plugin2 = new StubPanelPlugin { Id = "p2" };
        vm.AllPlugins = [plugin1, plugin2];
        var host = new MockPluginHost();
        var uiHost = new PluginUIHost(host);
        vm.PluginUI = uiHost;

        uiHost.TogglePanel(plugin1);
        Assert.Equal("p1", uiHost.ActivePluginId);

        uiHost.TogglePanel(plugin2);
        Assert.Equal("p2", uiHost.ActivePluginId);
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

    private class MockPluginHost : IPluginHost
    {
        public Model.ClipData? SelectedClip { get; set; }
        public IReadOnlyList<Model.ClipData> AllClips { get; set; } = [];
#pragma warning disable CS0067 // Events required by IPluginHost, not raised by this mock.
        public event EventHandler<Model.ClipData?>? SelectedClipChanged;
        public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
        public event EventHandler? ClipCollectionChanged;
#pragma warning restore CS0067
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public Model.ClipData? GetClip(string clipName) => null;
        public void UpdateClipXml(string clipName, string xml, string originPluginId) { }
        public void CreateClip(string name, string clipType, string? xml = null) { }
        public bool RemoveClip(string clipName) => false;
        public void UpdateSelectedClipXml(string xml, string originPluginId) { }
        public void ShowStatus(string message) { }
        public void RegisterRepository(Model.IClipRepository repository) { }
        public void RegisterTransform(IClipTransform transform) { }
        public Task<string?> ShowDialogAsync(string title, string message, string[] buttons) => Task.FromResult<string?>(null);
        public Task<string?> ShowInputDialogAsync(string title, string prompt, string? defaultValue = null) => Task.FromResult<string?>(null);
    }

    private class StubPanelPlugin : IPanelPlugin
    {
        public string Id { get; set; } = "stub";
        public string DisplayName => "Stub Plugin";
        public string Description => "";
        public string Version => "1.0.0-test";
        public IReadOnlyList<PluginKeyBinding> TestKeyBindings { get; set; } = [];
        public IReadOnlyList<PluginKeyBinding> KeyBindings => TestKeyBindings;
        public IReadOnlyList<PluginMenuAction> MenuActions => [];
        public PluginConfigSchema ConfigSchema => PluginConfigSchema.Empty;
        public void OnConfigChanged(IReadOnlyDictionary<string, object?> values) { }
        public Control CreatePanel() => new TextBlock { Text = "stub" };
        public void Initialize(IPluginHost host) { }
        public void Dispose() { }
    }

    [Fact]
    public void DeleteSelectedClip_ClearsSelectionBeforeRemoval()
    {
        var vm = CreateVm();
        vm.NewScriptCommand();
        Assert.NotNull(vm.SelectedClip);

        // Track that SelectedClip is nulled before collection changes
        bool selectedWasNullDuringRemove = false;
        vm.FileMakerClips.CollectionChanged += (_, _) =>
        {
            if (vm.SelectedClip == null)
                selectedWasNullDuringRemove = true;
        };

        vm.DeleteSelectedClip();

        Assert.Null(vm.SelectedClip);
        Assert.True(selectedWasNullDuringRemove,
            "SelectedClip should be null before the clip is removed from the collection");
    }

    [Fact]
    public void DeleteSelectedClip_NoSelection_DoesNotThrow()
    {
        var vm = CreateVm();
        vm.SelectedClip = null;
        vm.DeleteSelectedClip();
        Assert.Contains("No clip selected", vm.StatusMessage);
    }
}
