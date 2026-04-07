using SharpFM.Model;
using SharpFM.Plugin;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class PluginInterfaceTests
{
    // --- IPlugin hierarchy ---

    [Fact]
    public void IPanelPlugin_Extends_IPlugin()
    {
        Assert.True(typeof(IPlugin).IsAssignableFrom(typeof(IPanelPlugin)));
    }

    [Fact]
    public void IEventPlugin_Extends_IPlugin()
    {
        Assert.True(typeof(IPlugin).IsAssignableFrom(typeof(IEventPlugin)));
    }

    [Fact]
    public void IPersistencePlugin_Extends_IPlugin()
    {
        Assert.True(typeof(IPlugin).IsAssignableFrom(typeof(IPersistencePlugin)));
    }

    [Fact]
    public void IClipTransformPlugin_Extends_IPlugin()
    {
        Assert.True(typeof(IPlugin).IsAssignableFrom(typeof(IClipTransformPlugin)));
    }

    [Fact]
    public void IPanelPlugin_Extends_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IPanelPlugin)));
    }

    [Fact]
    public void IEventPlugin_Extends_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IEventPlugin)));
    }

    // --- ClipData record ---

    [Fact]
    public void ClipData_RecordEquality()
    {
        var a = new ClipData("Test", "Mac-XMSS", "<xml/>");
        var b = new ClipData("Test", "Mac-XMSS", "<xml/>");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ClipData_Properties()
    {
        var clip = new ClipData("MyClip", "Mac-XMTB", "<data/>");
        Assert.Equal("MyClip", clip.Name);
        Assert.Equal("Mac-XMTB", clip.ClipType);
        Assert.Equal("<data/>", clip.Xml);
    }

    // --- IPluginHost new members on MockPluginHost ---

    [Fact]
    public void MockPluginHost_AllClips_DefaultsToEmpty()
    {
        var host = new MockPluginHost();
        Assert.Empty(host.AllClips);
    }

    [Fact]
    public void MockPluginHost_ShowStatus_RecordsMessage()
    {
        var host = new MockPluginHost();
        host.ShowStatus("test message");
        Assert.Equal("test message", host.LastStatus);
    }

    [Fact]
    public void MockPluginHost_ClipCollectionChanged_Fires()
    {
        var host = new MockPluginHost();
        var fired = false;
        host.ClipCollectionChanged += (_, _) => fired = true;
        host.RaiseCollectionChanged();
        Assert.True(fired);
    }
}
