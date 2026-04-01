using System.IO;
using Microsoft.Extensions.Logging;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class PluginServiceInstallTests
{
    private static PluginService CreateService(string pluginsDir)
    {
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<PluginService>();
        return new PluginService(logger, pluginsDir);
    }

    [Fact]
    public void GetInstalledPluginFiles_EmptyDir_ReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            var service = CreateService(dir);
            Assert.Empty(service.GetInstalledPluginFiles());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetInstalledPluginFiles_MissingDir_ReturnsEmpty()
    {
        var service = CreateService("/tmp/nonexistent-sharpfm-plugins-" + Guid.NewGuid());
        Assert.Empty(service.GetInstalledPluginFiles());
    }

    [Fact]
    public void GetInstalledPluginFiles_ListsDlls()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "Plugin1.dll"), "fake");
            File.WriteAllText(Path.Combine(dir, "Plugin2.dll"), "fake");
            File.WriteAllText(Path.Combine(dir, "readme.txt"), "not a dll");

            var service = CreateService(dir);
            var files = service.GetInstalledPluginFiles();

            Assert.Equal(2, files.Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void InstallPlugin_CopiesFile()
    {
        var pluginsDir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        var sourceDir = Path.Combine(Path.GetTempPath(), $"sharpfm-source-{Guid.NewGuid()}");
        Directory.CreateDirectory(sourceDir);

        try
        {
            var sourceDll = Path.Combine(sourceDir, "FakePlugin.dll");
            File.WriteAllText(sourceDll, "fake dll content");

            var service = CreateService(pluginsDir);
            var host = new MockPluginHost();

            // Will fail to load as a real assembly, but the file should still be copied
            service.InstallPlugin(sourceDll, host);

            Assert.True(File.Exists(Path.Combine(pluginsDir, "FakePlugin.dll")));
        }
        finally
        {
            if (Directory.Exists(pluginsDir)) Directory.Delete(pluginsDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public void PluginsDirectory_IsAccessible()
    {
        var dir = "/tmp/test-plugins-" + Guid.NewGuid();
        var service = CreateService(dir);
        Assert.Equal(dir, service.PluginsDirectory);
    }
}
