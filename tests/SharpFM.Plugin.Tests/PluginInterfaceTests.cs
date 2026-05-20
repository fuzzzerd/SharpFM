using SharpFM.Model;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;
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
    public void IPlugin_Extends_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IPlugin)));
    }

    [Fact]
    public void IPanelPlugin_Extends_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(IPanelPlugin)));
    }

    // --- IUpdateCheckable opt-in capability ---

    [Fact]
    public void IUpdateCheckable_IsPublicInterface()
    {
        var t = typeof(IUpdateCheckable);
        Assert.True(t.IsInterface);
        Assert.True(t.IsPublic);
    }

    [Fact]
    public void IUpdateCheckable_DoesNotExtend_IPlugin()
    {
        // Capability interface, not a plugin sub-shape. A plugin opts in by
        // implementing both IPlugin and IUpdateCheckable.
        Assert.False(typeof(IPlugin).IsAssignableFrom(typeof(IUpdateCheckable)));
    }

    [Fact]
    public void IUpdateCheckable_DeclaresCheckForUpdatesAsync()
    {
        var method = typeof(IUpdateCheckable).GetMethod("CheckForUpdatesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<UpdateCheckResult>), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }

    [Fact]
    public void UpdateCheckResult_RecordEquality()
    {
        var a = new UpdateCheckResult(true, "2.0.0", new Uri("https://example.com/r/2.0.0"), "notes");
        var b = new UpdateCheckResult(true, "2.0.0", new Uri("https://example.com/r/2.0.0"), "notes");
        Assert.Equal(a, b);
    }

    [Fact]
    public void UpdateCheckResult_NullablesAllowed_WhenNoUpdate()
    {
        var none = new UpdateCheckResult(false, null, null, null);
        Assert.False(none.UpdateAvailable);
        Assert.Null(none.LatestVersion);
        Assert.Null(none.ReleaseUrl);
        Assert.Null(none.Notes);
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
