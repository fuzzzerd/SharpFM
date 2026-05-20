using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpFM.Plugin;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class AboutViewModelTests
{
    [Fact]
    public void Host_AlwaysCheckable()
    {
        var vm = new AboutViewModel(
            "SharpFM", "2.0.0", new StubChecker(),
            new Uri("https://example.invalid"),
            Array.Empty<IPlugin>());

        Assert.True(vm.Host.CanCheckForUpdates);
    }

    [Fact]
    public void Plugins_WithoutUpdateChannel_AreNotCheckable()
    {
        var plugins = new IPlugin[] { new BundledPlugin("Bundled", "1.0.0") };

        var vm = new AboutViewModel(
            "SharpFM", "2.0.0", new StubChecker(),
            new Uri("https://example.invalid"),
            plugins);

        Assert.Single(vm.Plugins);
        Assert.False(vm.Plugins[0].CanCheckForUpdates);
        Assert.Equal("Bundled", vm.Plugins[0].DisplayName);
        Assert.Equal("1.0.0", vm.Plugins[0].Version);
    }

    [Fact]
    public void Plugins_WithUpdateChannel_AreCheckable()
    {
        var plugins = new IPlugin[] { new UpdatablePlugin("Updatable", "1.0.0") };

        var vm = new AboutViewModel(
            "SharpFM", "2.0.0", new StubChecker(),
            new Uri("https://example.invalid"),
            plugins);

        Assert.Single(vm.Plugins);
        Assert.True(vm.Plugins[0].CanCheckForUpdates);
    }

    [Fact]
    public void HostHomepage_IsExposed()
    {
        var url = new Uri("https://example.invalid/releases");
        var vm = new AboutViewModel(
            "SharpFM", "2.0.0", new StubChecker(),
            url, Array.Empty<IPlugin>());

        Assert.Equal(url, vm.HostHomepageUrl);
    }

    private sealed class StubChecker : IUpdateCheckable
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct) =>
            Task.FromResult(new UpdateCheckResult(false, null, null, null));
    }

    private class BundledPlugin(string name, string version) : IPlugin
    {
        public string Id => name;
        public string DisplayName => name;
        public string Description => "";
        public string Version { get; } = version;
        public IReadOnlyList<PluginKeyBinding> KeyBindings => Array.Empty<PluginKeyBinding>();
        public IReadOnlyList<PluginMenuAction> MenuActions => Array.Empty<PluginMenuAction>();
        public PluginConfigSchema ConfigSchema => PluginConfigSchema.Empty;
        public void Initialize(IPluginHost host) { }
        public void OnConfigChanged(IReadOnlyDictionary<string, object?> values) { }
        public void Dispose() { }
    }

    private sealed class UpdatablePlugin(string name, string version) : BundledPlugin(name, version), IUpdateCheckable
    {
        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct) =>
            Task.FromResult(new UpdateCheckResult(false, null, null, null));
    }
}
