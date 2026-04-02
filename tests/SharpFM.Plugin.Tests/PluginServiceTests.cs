using System.IO;
using Microsoft.Extensions.Logging;
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
    private static PluginService CreateService(string pluginsDir)
    {
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<PluginService>();
        return new PluginService(logger, pluginsDir);
    }

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
        Directory.CreateDirectory(dir);

        try
        {
            var service = CreateService(dir);
            service.LoadPlugins(new MockPluginHost());
            Assert.Empty(service.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadPlugins_InvalidDll_GracefullySkips()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "BadPlugin.dll"), "not a real assembly");

            var service = CreateService(dir);
            service.LoadPlugins(new MockPluginHost());

            Assert.Empty(service.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadPlugins_DllWithNoPluginTypes_LoadsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            // Copy a real assembly that has no IPanelPlugin implementations
            var nlogAssembly = typeof(NLog.LogManager).Assembly.Location;
            File.Copy(nlogAssembly, Path.Combine(dir, "NLog.dll"));

            var service = CreateService(dir);
            service.LoadPlugins(new MockPluginHost());

            Assert.Empty(service.LoadedPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void InstallPlugin_InvalidDll_CopiesButReturnsEmpty()
    {
        var pluginsDir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        var sourceDir = Path.Combine(Path.GetTempPath(), $"sharpfm-source-{Guid.NewGuid()}");
        Directory.CreateDirectory(sourceDir);

        try
        {
            var sourceDll = Path.Combine(sourceDir, "Bad.dll");
            File.WriteAllText(sourceDll, "not a real assembly");

            var service = CreateService(pluginsDir);
            var result = service.InstallPlugin(sourceDll, new MockPluginHost());

            Assert.Empty(result);
            Assert.True(File.Exists(Path.Combine(pluginsDir, "Bad.dll")));
        }
        finally
        {
            if (Directory.Exists(pluginsDir)) Directory.Delete(pluginsDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public void InstallPlugin_OverwritesExisting()
    {
        var pluginsDir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        var sourceDir = Path.Combine(Path.GetTempPath(), $"sharpfm-source-{Guid.NewGuid()}");
        Directory.CreateDirectory(pluginsDir);
        Directory.CreateDirectory(sourceDir);

        try
        {
            File.WriteAllText(Path.Combine(pluginsDir, "Plugin.dll"), "old content");
            var sourceDll = Path.Combine(sourceDir, "Plugin.dll");
            File.WriteAllText(sourceDll, "new content");

            var service = CreateService(pluginsDir);
            service.InstallPlugin(sourceDll, new MockPluginHost());

            Assert.Equal("new content", File.ReadAllText(Path.Combine(pluginsDir, "Plugin.dll")));
        }
        finally
        {
            Directory.Delete(pluginsDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public void LoadPlugins_WithRealPlugin_LoadsIt()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            // Copy the sample plugin and its dependencies
            var sampleAssembly = typeof(SharpFM.Plugin.Sample.ClipInspectorPlugin).Assembly.Location;
            var sampleDir = Path.GetDirectoryName(sampleAssembly)!;
            File.Copy(sampleAssembly, Path.Combine(dir, Path.GetFileName(sampleAssembly)));

            var service = CreateService(dir);
            service.LoadPlugins(new MockPluginHost());

            Assert.Single(service.LoadedPlugins);
            Assert.Equal("clip-inspector", service.LoadedPlugins[0].Id);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
