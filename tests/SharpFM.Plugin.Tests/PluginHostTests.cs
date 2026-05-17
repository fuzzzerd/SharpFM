using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
using SharpFM.Model.Parsing;
using SharpFM.Plugin;
using SharpFM.Services;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class MockClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
    public Task SetDataAsync(string format, byte[] data) => Task.CompletedTask;
    public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());
    public Task<object?> GetDataAsync(string format) => Task.FromResult<object?>(null);
}

public class MockFolderService : IFolderService
{
    public Task<string> GetFolderAsync() => Task.FromResult("/tmp/test");
}

public class PluginHostTests
{
    private static MainWindowViewModel CreateVm()
    {
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<MainWindowViewModel>();
        return new MainWindowViewModel(logger, new MockClipboardService(), new MockFolderService());
    }

    [Fact]
    public void SelectedClip_ReturnsNull_WhenNoClipSelected()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        Assert.Null(host.SelectedClip);
    }

    [Fact]
    public void SelectedClip_ReturnsClipData_WhenClipSelected()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        vm.NewScriptCommand();

        Assert.NotNull(host.SelectedClip);
        Assert.Equal("New Script", host.SelectedClip!.Name);
        Assert.Equal("Mac-XMSS", host.SelectedClip.ClipType);
    }

    [Fact]
    public void SelectedClipChanged_Fires_WhenSelectionChanges()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        ClipData? received = null;
        host.SelectedClipChanged += (_, clip) => received = clip;

        vm.NewScriptCommand();

        Assert.NotNull(received);
        Assert.Equal("New Script", received!.Name);
    }

    [Fact]
    public void SelectedClipChanged_FiresNull_WhenDeselected()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        vm.NewScriptCommand();

        ClipData? received = new ClipData("placeholder", "", "");
        host.SelectedClipChanged += (_, clip) => received = clip;
        vm.SelectedClip = null;

        Assert.Null(received);
    }

    [Fact]
    public void UpdateSelectedClipXml_UpdatesClipContent()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        vm.NewScriptCommand();
        var newXml = "<fmxmlsnippet type=\"FMObjectList\"><test /></fmxmlsnippet>";
        host.UpdateSelectedClipXml(newXml, "test-plugin");

        // Clip.Xml is canonicalised via PrettyPrint; assert structural equivalence.
        Assert.Contains("<test", vm.SelectedClip!.Clip.Xml);
        Assert.Contains("fmxmlsnippet", vm.SelectedClip.Clip.Xml);
    }

    [Fact]
    public void UpdateSelectedClipXml_NoOp_WhenNoClipSelected()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        // Should not throw
        host.UpdateSelectedClipXml("<test />", "test-plugin");
    }

    [Fact]
    public void SelectedClip_ReturnsNull_WhenNoClip()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        Assert.Null(host.SelectedClip);
    }

    [Fact]
    public void SelectedClip_ReturnsFreshClipData()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        vm.NewScriptCommand();
        var clip = host.SelectedClip;

        Assert.NotNull(clip);
        Assert.Equal("New Script", clip!.Name);
        Assert.Equal("Mac-XMSS", clip.ClipType);
    }

    [Fact]
    public void SelectedClipChanged_IncludesSyncedXml_AfterSwitch()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        // Create two clips
        vm.NewScriptCommand();
        var scriptClip = vm.SelectedClip!;
        vm.NewTableCommand();

        // Switch back to the script clip
        ClipData? received = null;
        host.SelectedClipChanged += (_, clip) => received = clip;
        vm.SelectedClip = scriptClip;

        Assert.NotNull(received);
        Assert.Equal("New Script", received!.Name);
    }

    // --- New IPluginHost members ---

    [Fact]
    public void AllClips_ReturnsAllLoadedClips()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        var baseline = host.AllClips.Count;
        vm.NewScriptCommand();
        vm.NewTableCommand();

        Assert.Equal(baseline + 2, host.AllClips.Count);
    }

    [Fact]
    public void ClipCollectionChanged_Fires_WhenClipAdded()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        var fired = false;
        host.ClipCollectionChanged += (_, _) => fired = true;

        vm.NewScriptCommand();

        Assert.True(fired);
    }

    [Fact]
    public void ShowStatus_SetsViewModelStatus()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        host.ShowStatus("Plugin says hello");

        Assert.Equal("Plugin says hello", vm.StatusMessage);
    }

    // --- Item 1: ClipData.ParseReport ---

    [Fact]
    public void SelectedClip_ExposesParseReport_FromClip()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        vm.NewScriptCommand();

        Assert.NotNull(host.SelectedClip);
        Assert.NotNull(host.SelectedClip!.ParseReport);
        Assert.True(host.SelectedClip.ParseReport.IsLossless);
    }

    [Fact]
    public void SelectedClip_ParseReport_SurfacesDiagnostics()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        vm.NewScriptCommand();

        host.UpdateSelectedClipXml(
            "<fmxmlsnippet type=\"FMObjectList\"><Mystery /></fmxmlsnippet>",
            "test-plugin");

        var report = host.SelectedClip!.ParseReport;
        Assert.False(report.IsLossless);
        Assert.NotEmpty(report.Diagnostics);
    }

    [Fact]
    public void AllClips_PopulatesParseReport()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        vm.NewScriptCommand();
        vm.NewTableCommand();

        Assert.All(host.AllClips, c => Assert.NotNull(c.ParseReport));
    }

    [Fact]
    public void GetClip_PopulatesParseReport()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        vm.NewScriptCommand();

        var clip = host.GetClip("New Script");

        Assert.NotNull(clip);
        Assert.NotNull(clip!.ParseReport);
    }

    [Fact]
    public void ClipContentChanged_FromUpdate_CarriesParseReport()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        vm.NewScriptCommand();
        ClipContentChangedArgs? args = null;
        host.ClipContentChanged += (_, e) => args = e;

        host.UpdateSelectedClipXml(
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>",
            "test-plugin");

        Assert.NotNull(args);
        Assert.NotNull(args!.Clip.ParseReport);
    }

    // --- Item 2: ValidateClipXml ---

    [Fact]
    public void ValidateClipXml_Lossless_OnCleanScriptStepsXml()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        var report = host.ValidateClipXml(
            "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>");

        Assert.True(report.IsLossless);
    }

    [Fact]
    public void ValidateClipXml_ReportsXmlMalformed_OnGarbage()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        var report = host.ValidateClipXml("Mac-XMSS", "<oops>");

        Assert.False(report.IsLossless);
        Assert.Contains(
            report.Diagnostics,
            d => d.Kind == ParseDiagnosticKind.XmlMalformed);
    }

    [Fact]
    public void ValidateClipXml_FallsBackToOpaque_OnUnknownType()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);

        var report = host.ValidateClipXml("Mac-XMUNKNOWN", "<root/>");

        Assert.True(report.IsLossless);
    }

    [Fact]
    public void ValidateClipXml_DoesNotMutateClipCollection()
    {
        var vm = CreateVm();
        var host = new PluginHost(vm, NullLoggerFactory.Instance);
        var beforeCount = host.AllClips.Count;

        host.ValidateClipXml("Mac-XMSS", "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>");

        Assert.Equal(beforeCount, host.AllClips.Count);
    }
}
