using SharpFM.Model;
using SharpFM.Plugin;
using SharpFM.Plugin.Sample;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class ClipInspectorPluginTests
{
    [Fact]
    public void Plugin_HasCorrectMetadata()
    {
        using var plugin = new ClipInspectorPlugin();

        Assert.Equal("clip-inspector", plugin.Id);
        Assert.Equal("Clip Inspector", plugin.DisplayName);
    }

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        using var plugin = new ClipInspectorPlugin();
        var host = new MockPluginHost();

        plugin.Initialize(host);
    }

    [Fact]
    public void ViewModel_UpdatesFromClipData()
    {
        var vm = new ClipInspectorViewModel();

        Assert.False(vm.HasClip);

        var clip = new ClipData("TestClip", "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"><Step enable=\"True\"><StepId>89</StepId></Step></fmxmlsnippet>");
        vm.Update(clip);

        Assert.True(vm.HasClip);
        Assert.Equal("TestClip", vm.ClipName);
        Assert.Equal("Mac-XMSS", vm.ClipType);
        Assert.NotEqual("-", vm.ElementCount);
        Assert.NotEqual("-", vm.XmlSize);
    }

    [Fact]
    public void ViewModel_ClearsOnNull()
    {
        var vm = new ClipInspectorViewModel();
        vm.Update(new ClipData("Test", "Mac-XMSS", "<root/>"));

        Assert.True(vm.HasClip);

        vm.Update(null);

        Assert.False(vm.HasClip);
        Assert.Equal("-", vm.ElementCount);
    }

    [Fact]
    public void ViewModel_HandlesInvalidXml()
    {
        var vm = new ClipInspectorViewModel();
        vm.Update(new ClipData("Bad", "Mac-XMSS", "not xml at all"));

        Assert.Equal("(invalid XML)", vm.ElementCount);
    }

    [Fact]
    public void Plugin_ReceivesClipChanges()
    {
        using var plugin = new ClipInspectorPlugin();
        var host = new MockPluginHost();
        plugin.Initialize(host);

        // CreatePanel wires up the ViewModel
        var panel = plugin.CreatePanel();
        Assert.NotNull(panel);

        // Simulate clip change
        var clip = new ClipData("Changed", "Mac-XMTB", "<root><child/></root>");
        host.RaiseChanged(clip);

        // The plugin's internal ViewModel should have updated
        var vm = panel.DataContext as ClipInspectorViewModel;
        Assert.NotNull(vm);
        Assert.Equal("Changed", vm!.ClipName);
    }
}
