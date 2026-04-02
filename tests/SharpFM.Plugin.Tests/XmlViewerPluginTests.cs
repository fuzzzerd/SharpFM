using SharpFM.Plugin;
using SharpFM.Plugin.XmlViewer;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class XmlViewerPluginTests
{
    [Fact]
    public void Plugin_HasCorrectMetadata()
    {
        using var plugin = new XmlViewerPlugin();
        Assert.Equal("xml-viewer", plugin.Id);
        Assert.Equal("XML Viewer", plugin.DisplayName);
    }

    [Fact]
    public void Plugin_RegistersCtrlShiftXKeybinding()
    {
        using var plugin = new XmlViewerPlugin();
        Assert.Single(plugin.KeyBindings);
        Assert.Equal("Ctrl+Shift+X", plugin.KeyBindings[0].Gesture);
    }

    // NOTE: CreatePanel() requires an Avalonia app context (TextMate/AvaloniaEdit init).
    // The ViewModel tests below cover the logic without requiring a UI host.

    [Fact]
    public void ViewModel_LoadClip_SetsHasClip()
    {
        var host = new MockPluginHost();
        var vm = new XmlViewerViewModel(host, "test");

        Assert.False(vm.HasClip);

        var clip = new ClipInfo("Test", "Mac-XMSS", "<root/>");
        vm.LoadClip(clip);

        Assert.True(vm.HasClip);
        Assert.Equal("Test (Mac-XMSS)", vm.ClipLabel);
        Assert.Equal("<root/>", vm.Document.Text);
    }

    [Fact]
    public void ViewModel_LoadClip_Null_ClearsState()
    {
        var host = new MockPluginHost();
        var vm = new XmlViewerViewModel(host, "test");
        vm.LoadClip(new ClipInfo("Test", "Mac-XMSS", "<root/>"));

        vm.LoadClip(null);

        Assert.False(vm.HasClip);
        Assert.Equal("", vm.Document.Text);
    }

    [Fact]
    public void ViewModel_RefreshFromHost_SyncsClipData()
    {
        var host = new MockPluginHost
        {
            SelectedClip = new ClipInfo("Synced", "Mac-XMTB", "<table/>")
        };
        var vm = new XmlViewerViewModel(host, "test");

        vm.RefreshFromHost();

        Assert.True(vm.HasClip);
        Assert.Contains("Synced", vm.ClipLabel);
        Assert.Equal("<table/>", vm.Document.Text);
    }

    [Fact]
    public void ViewModel_SyncToHost_PushesXmlWithOrigin()
    {
        var host = new TrackingPluginHost();
        host.SelectedClip = new ClipInfo("Test", "Mac-XMSS", "<original/>");

        var vm = new XmlViewerViewModel(host, "xml-viewer");
        vm.LoadClip(host.SelectedClip);

        vm.Document.Text = "<modified/>";
        vm.SyncToHost();

        Assert.Equal("<modified/>", host.LastUpdatedXml);
        Assert.Equal("xml-viewer", host.LastOriginPluginId);
    }

    [Fact]
    public void Plugin_Initialize_SubscribesToClipChanges()
    {
        using var plugin = new XmlViewerPlugin();
        var host = new MockPluginHost();
        plugin.Initialize(host);

        // Raising SelectedClipChanged should not throw even before CreatePanel
        var clip = new ClipInfo("NewClip", "Mac-XMSC", "<script/>");
        host.RaiseChanged(clip);
    }

    [Fact]
    public void Plugin_Dispose_UnsubscribesFromHost()
    {
        var plugin = new XmlViewerPlugin();
        var host = new MockPluginHost();
        plugin.Initialize(host);
        plugin.Dispose();

        // After dispose, raising the event should be a no-op (no subscribers)
        host.RaiseChanged(new ClipInfo("After", "Mac-XMSS", "<test/>"));
    }

    [Fact]
    public void ViewModel_UpdatesOnClipContentChanged()
    {
        var host = new MockPluginHost
        {
            SelectedClip = new ClipInfo("Test", "Mac-XMSS", "<original/>")
        };
        var vm = new XmlViewerViewModel(host, "test");
        vm.LoadClip(host.SelectedClip);

        Assert.Equal("<original/>", vm.Document.Text);

        // Simulate a content change (e.g. user edited in script editor, host debounced and synced)
        var updated = new ClipInfo("Test", "Mac-XMSS", "<updated-from-editor/>");
        vm.LoadClip(updated);

        Assert.Equal("<updated-from-editor/>", vm.Document.Text);
    }
}

public class TrackingPluginHost : IPluginHost
{
    public ClipInfo? SelectedClip { get; set; }
    public string? LastUpdatedXml { get; private set; }
    public string? LastOriginPluginId { get; private set; }
    public event EventHandler<ClipInfo?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;

    public void UpdateSelectedClipXml(string xml, string originPluginId)
    {
        LastUpdatedXml = xml;
        LastOriginPluginId = originPluginId;
    }

    public ClipInfo? RefreshSelectedClip() => SelectedClip;
    public void RaiseChanged(ClipInfo? clip) => SelectedClipChanged?.Invoke(this, clip);
    public void RaiseContentChanged(ClipContentChangedArgs args) => ClipContentChanged?.Invoke(this, args);
}
