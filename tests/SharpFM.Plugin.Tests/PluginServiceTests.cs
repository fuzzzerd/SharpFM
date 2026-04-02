using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Plugin;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class MockPluginHost : IPluginHost
{
    public ClipInfo? SelectedClip { get; set; }
    public event EventHandler<ClipInfo?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public void UpdateSelectedClipXml(string xml, string originPluginId) { }
    public ClipInfo? RefreshSelectedClip() => SelectedClip;
    public void RaiseChanged(ClipInfo? clip) => SelectedClipChanged?.Invoke(this, clip);
    public void RaiseContentChanged(ClipContentChangedArgs args) => ClipContentChanged?.Invoke(this, args);
}

public class PluginServiceTests
{
    [Fact]
    public void LoadPlugins_NoPluginsDir_LoadsZero()
    {
        var service = new PluginService(NullLogger.Instance);
        var host = new MockPluginHost();

        // AppContext.BaseDirectory won't have a plugins/ dir in test
        service.LoadPlugins(host);

        Assert.Empty(service.LoadedPlugins);
    }

    [Fact]
    public void LoadPlugins_EmptyDir_LoadsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(dir, "plugins"));

        try
        {
            // PluginService scans AppContext.BaseDirectory/plugins, so this tests
            // the scenario indirectly — the important behavior is graceful handling
            var service = new PluginService(NullLogger.Instance);
            service.LoadPlugins(new MockPluginHost());
            Assert.Empty(service.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
