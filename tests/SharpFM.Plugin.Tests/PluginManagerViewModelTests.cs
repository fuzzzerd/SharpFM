using Avalonia.Controls;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;
using SharpFM.PluginManager;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class PluginManagerViewModelTests
{
    private class StubPlugin : IPanelPlugin
    {
        public string Id { get; set; } = "stub";
        public string DisplayName { get; set; } = "Stub";
        public string Description => "";
        public string Version => "1.0.0-test";
        public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
        public IReadOnlyList<PluginMenuAction> MenuActions => [];
        public PluginConfigSchema ConfigSchema { get; set; } = PluginConfigSchema.Empty;
        public void OnConfigChanged(IReadOnlyDictionary<string, object?> values) { }
        public Control CreatePanel() => new TextBlock();
        public void Initialize(IPluginHost host) { }
        public void Dispose() { }
    }

    [Fact]
    public void Refresh_PopulatesPlugins()
    {
        var vm = new PluginManagerViewModel();
        var plugins = new List<IPlugin> { new StubPlugin() };

        vm.Refresh(plugins, activePluginId: null);

        Assert.Single(vm.Plugins);
        Assert.Equal("stub", vm.Plugins[0].Id);
        Assert.False(vm.Plugins[0].IsActive);
    }

    [Fact]
    public void Refresh_MarksActivePlugin()
    {
        var vm = new PluginManagerViewModel();
        var plugin = new StubPlugin { Id = "active-one" };
        var plugins = new List<IPlugin> { plugin, new StubPlugin { Id = "other" } };

        vm.Refresh(plugins, activePluginId: "active-one");

        Assert.True(vm.Plugins[0].IsActive);
        Assert.False(vm.Plugins[1].IsActive);
    }

    [Fact]
    public void Refresh_ClearsPreviousEntries()
    {
        var vm = new PluginManagerViewModel();
        vm.Refresh(new List<IPlugin> { new StubPlugin() }, null);
        Assert.Single(vm.Plugins);

        vm.Refresh(new List<IPlugin>(), null);
        Assert.Empty(vm.Plugins);
    }

    [Fact]
    public void PluginEntry_Description()
    {
        var entry = new PluginEntry(new StubPlugin(), false);
        Assert.Equal("", entry.Description);
    }

    [Fact]
    public void SelectedPlugin_UpdatesHasSelection()
    {
        var vm = new PluginManagerViewModel();
        Assert.False(vm.HasSelection);

        var entry = new PluginEntry(new StubPlugin(), false);
        vm.Plugins.Add(entry);
        vm.SelectedPlugin = entry;

        Assert.True(vm.HasSelection);

        vm.SelectedPlugin = null;
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void PluginEntry_ExposesMetadata()
    {
        var plugin = new StubPlugin { Id = "test-id", DisplayName = "Test Plugin" };
        var entry = new PluginEntry(plugin, isActive: true);

        Assert.Equal("test-id", entry.Id);
        Assert.Equal("Test Plugin", entry.DisplayName);
        Assert.True(entry.IsActive);
        Assert.NotNull(entry.AssemblyName);
    }
}
