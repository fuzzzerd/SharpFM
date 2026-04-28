using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class OpenTabsViewModelTests
{
    private static ClipViewModel Clip(string name) =>
        new(SharpFM.Model.Clip.FromXml(name, "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"><Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step></fmxmlsnippet>"));

    [Fact]
    public void OpenAsPreview_CreatesPreviewTab_FirstTime()
    {
        var tabs = new OpenTabsViewModel();
        var tab = tabs.OpenAsPreview(Clip("A"));

        Assert.True(tab.IsPreview);
        Assert.Same(tab, tabs.PreviewTab);
        Assert.Same(tab, tabs.ActiveTab);
        Assert.Single(tabs.Tabs);
    }

    [Fact]
    public void OpenAsPreview_ReusesSlot_WhenAnotherPreviewExists()
    {
        var tabs = new OpenTabsViewModel();
        var first = tabs.OpenAsPreview(Clip("A"));
        var secondClip = Clip("B");

        var second = tabs.OpenAsPreview(secondClip);

        Assert.Same(first, second); // reused the same tab instance
        Assert.Same(secondClip, second.Clip);
        Assert.Single(tabs.Tabs);
        Assert.True(second.IsPreview);
    }

    [Fact]
    public void OpenAsPreview_ActivatesExistingTab_WithoutChangingIt()
    {
        var tabs = new OpenTabsViewModel();
        var clip = Clip("A");
        var tab = tabs.OpenAsPermanent(clip);
        tabs.OpenAsPreview(Clip("B")); // another preview, shouldn't affect A

        tabs.OpenAsPreview(clip);

        Assert.Same(tab, tabs.ActiveTab);
        Assert.False(tab.IsPreview); // still permanent
    }

    [Fact]
    public void OpenAsPermanent_GraduatesExistingPreview()
    {
        var tabs = new OpenTabsViewModel();
        var clip = Clip("A");
        var preview = tabs.OpenAsPreview(clip);

        var promoted = tabs.OpenAsPermanent(clip);

        Assert.Same(preview, promoted);
        Assert.False(promoted.IsPreview);
        Assert.Null(tabs.PreviewTab);
    }

    [Fact]
    public void GraduateActive_ClearsPreviewFlag()
    {
        var tabs = new OpenTabsViewModel();
        tabs.OpenAsPreview(Clip("A"));
        tabs.GraduateActive();

        Assert.False(tabs.ActiveTab!.IsPreview);
        Assert.Null(tabs.PreviewTab);
    }

    [Fact]
    public void EditingPreviewClip_GraduatesTab()
    {
        var tabs = new OpenTabsViewModel();
        var clip = Clip("A");
        var tab = tabs.OpenAsPreview(clip);

        clip.ScriptDocument!.Text += "\n# edited";
        // The editor's ContentChanged event is debounced via the UI dispatcher;
        // drive the same handler synchronously for the test.
        clip.HandleEditorContentChanged();

        Assert.True(clip.IsDirty);
        Assert.False(tab.IsPreview);
        Assert.Null(tabs.PreviewTab);
    }

    [Fact]
    public void Close_PicksNeighbour_WhenClosingActive()
    {
        var tabs = new OpenTabsViewModel();
        var a = tabs.OpenAsPermanent(Clip("A"));
        var b = tabs.OpenAsPermanent(Clip("B"));
        var c = tabs.OpenAsPermanent(Clip("C"));

        tabs.ActiveTab = b;
        tabs.Close(b);

        Assert.Same(c, tabs.ActiveTab); // next neighbour preferred
        Assert.DoesNotContain(b, tabs.Tabs);

        tabs.Close(c);
        Assert.Same(a, tabs.ActiveTab); // falls back to previous when no next
    }

    [Fact]
    public void Close_LastTab_NullsActive()
    {
        var tabs = new OpenTabsViewModel();
        var a = tabs.OpenAsPreview(Clip("A"));

        tabs.Close(a);

        Assert.Null(tabs.ActiveTab);
        Assert.Null(tabs.PreviewTab);
        Assert.Empty(tabs.Tabs);
    }

    [Fact]
    public void CloseClip_RemovesAllTabsForThatClip()
    {
        var tabs = new OpenTabsViewModel();
        var clip = Clip("A");
        tabs.OpenAsPermanent(clip);

        tabs.CloseClip(clip);

        Assert.Empty(tabs.Tabs);
    }
}
