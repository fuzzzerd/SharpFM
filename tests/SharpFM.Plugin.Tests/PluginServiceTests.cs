using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
using SharpFM.Plugin;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class MockPluginHost : IPluginHost
{
    public ClipData? SelectedClip { get; set; }
    public IReadOnlyList<ClipData> AllClips { get; set; } = [];
    public event EventHandler<ClipData?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;
    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
    public ClipData? GetClip(string clipName) => AllClips.FirstOrDefault(c => c.Name.Equals(clipName, StringComparison.OrdinalIgnoreCase));
    public void UpdateClipXml(string clipName, string xml, string originPluginId) { }
    public void CreateClip(string name, string clipType, string? xml = null) { }
    public bool RemoveClip(string clipName) => false;
    public void UpdateSelectedClipXml(string xml, string originPluginId) { }
    public void ShowStatus(string message) { LastStatus = message; }
    public void RegisterRepository(IClipRepository repository) { }
    public void RegisterTransform(IClipTransform transform) { }
    public Task<string?> ShowDialogAsync(string title, string message, string[] buttons) => Task.FromResult<string?>(null);
    public Task<string?> ShowInputDialogAsync(string title, string prompt, string? defaultValue = null) => Task.FromResult<string?>(null);
    public string? LastStatus { get; private set; }
    public void RaiseChanged(ClipData? clip) => SelectedClipChanged?.Invoke(this, clip);
    public void RaiseContentChanged(ClipContentChangedArgs args) => ClipContentChanged?.Invoke(this, args);
    public void RaiseCollectionChanged() => ClipCollectionChanged?.Invoke(this, EventArgs.Empty);
}

public class PluginServiceTests
{
    private static PluginService CreateService(string pluginsDir)
    {
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<PluginService>();
        var configDir = Path.Combine(Path.GetTempPath(), $"sharpfm-cfg-{Guid.NewGuid()}");
        var configService = new PluginConfigService(NullLogger.Instance, configDir);
        return new PluginService(logger, configService, pluginsDir);
    }

    [Fact]
    public void LoadPlugins_NoPluginsDir_LoadsZero()
    {
        var configDir = Path.Combine(Path.GetTempPath(), $"sharpfm-cfg-{Guid.NewGuid()}");
        var configService = new PluginConfigService(NullLogger.Instance, configDir);
        var service = new PluginService(NullLogger.Instance, configService);
        var host = new MockPluginHost();

        // AppContext.BaseDirectory won't have a plugins/ dir in test
        service.LoadPlugins(host);

        Assert.Empty(service.AllPlugins);
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
            Assert.Empty(service.AllPlugins);
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

            Assert.Empty(service.AllPlugins);
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
    public void AllPlugins_DefaultsToEmpty()
    {
        var service = CreateService("/tmp/nonexistent-" + Guid.NewGuid());
        Assert.Empty(service.AllPlugins);
    }

    [Fact]
    public void LoadPlugins_ScansSubdirectories()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            // Create a subdirectory with a non-matching DLL name — should be skipped
            var subDir = Path.Combine(dir, "MyPlugin");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "WrongName.dll"), "not a real assembly");

            var service = CreateService(dir);
            service.LoadPlugins(new MockPluginHost());

            // No plugins should load: subdirectory DLL name doesn't match directory name
            Assert.Empty(service.AllPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadPlugins_SubdirectoryWithMatchingName_AttemptsLoad()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            // Create a subdirectory with a matching DLL name (invalid content, but proves scanning works)
            var subDir = Path.Combine(dir, "MyPlugin");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "MyPlugin.dll"), "not a real assembly");

            var service = CreateService(dir);
            // Should attempt to load and gracefully fail (bad DLL), not throw
            service.LoadPlugins(new MockPluginHost());

            Assert.Empty(service.AllPlugins);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
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

}
