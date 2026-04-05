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
    public IReadOnlyList<ClipInfo> AllClips { get; set; } = [];
    public event EventHandler<ClipInfo?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;
    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
    public ClipInfo? GetClip(string clipName) => AllClips.FirstOrDefault(c => c.Name.Equals(clipName, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<StepCatalogEntry> GetAvailableSteps(string? category = null) => [];
    public StepCatalogEntry? GetStepDefinition(string stepName) => null;
    public IReadOnlyList<ScriptStepInfo>? GetScriptSteps(string clipName) => null;
    public IReadOnlyList<string> UpdateScriptSteps(string clipName, IReadOnlyList<ScriptStepOperation> operations, string originPluginId) => [];
    public IReadOnlyList<FieldInfo>? GetTableFields(string clipName) => null;
    public IReadOnlyList<string> UpdateTableFields(string clipName, IReadOnlyList<FieldOperation> operations, string originPluginId) => [];
    public void UpdateClipXml(string clipName, string xml, string originPluginId) { }
    public void CreateClip(string name, string clipType, string? xml = null) { }
    public bool RemoveClip(string clipName) => false;
    public void UpdateSelectedClipXml(string xml, string originPluginId) { }
    public ClipInfo? RefreshSelectedClip() => SelectedClip;
    public void ShowStatus(string message) { LastStatus = message; }
    public string? LastStatus { get; private set; }
    public void RaiseChanged(ClipInfo? clip) => SelectedClipChanged?.Invoke(this, clip);
    public void RaiseContentChanged(ClipContentChangedArgs args) => ClipContentChanged?.Invoke(this, args);
    public void RaiseCollectionChanged() => ClipCollectionChanged?.Invoke(this, EventArgs.Empty);
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
    public void AllPlugins_AggregatesAllTypes()
    {
        var service = CreateService("/tmp/nonexistent-" + Guid.NewGuid());
        // No plugins loaded, but verify the property returns empty aggregate
        Assert.Empty(service.AllPlugins);
        Assert.Empty(service.PanelPlugins);
        Assert.Empty(service.EventPlugins);
        Assert.Empty(service.PersistencePlugins);
        Assert.Empty(service.TransformPlugins);
    }

    [Fact]
    public void LoadedPlugins_ReturnsPanelPlugins()
    {
        var service = CreateService("/tmp/nonexistent-" + Guid.NewGuid());
        // LoadedPlugins is a backwards-compat alias for PanelPlugins
        Assert.Same(service.PanelPlugins, service.LoadedPlugins);
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
