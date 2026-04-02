using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ClipViewModelTests
{
    private static string WrapXml(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    private static ClipViewModel CreateScriptClip(string xml)
    {
        var clip = new FileMakerClip("Test", "Mac-XMSS", xml);
        return new ClipViewModel(clip);
    }

    [Fact]
    public void IsScriptClip_TrueForXMSS()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"));
        Assert.True(vm.IsScriptClip);
    }

    [Fact]
    public void IsScriptClip_FalseForTable()
    {
        var clip = new FileMakerClip("Test", "Mac-XMTB", "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>");
        var vm = new ClipViewModel(clip);
        Assert.False(vm.IsScriptClip);
    }

    [Fact]
    public void ScriptDocument_LazyCreated()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello</Text></Step>");
        var vm = CreateScriptClip(xml);

        // Access ScriptDocument triggers lazy creation
        var doc = vm.ScriptDocument;
        Assert.NotNull(doc);
        Assert.Contains("# hello", doc.Text);
    }

    [Fact]
    public void SyncModelFromEditor_UpdatesXml()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>original</Text></Step>");
        var vm = CreateScriptClip(xml);

        // Access the script document and modify it
        var doc = vm.ScriptDocument;
        doc.Text = "# modified";

        vm.SyncModelFromEditor();

        Assert.Contains("modified", vm.ClipXml);
        Assert.Contains("modified", vm.Clip.XmlData);
    }

    [Fact]
    public void SyncEditorFromXml_UpdatesScriptDocument()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>original</Text></Step>");
        var vm = CreateScriptClip(xml);

        // Access script document first
        _ = vm.ScriptDocument;

        // Change the XML directly
        vm.ClipXml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>changed via xml</Text></Step>");

        vm.SyncEditorFromXml();

        Assert.Contains("changed via xml", vm.ScriptDocument.Text);
    }

    [Fact]
    public void SyncModelFromEditor_TableClip_RoundTripsXml()
    {
        var clip = new FileMakerClip("Test", "Mac-XMTB", "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"T\"></BaseTable></fmxmlsnippet>");
        var vm = new ClipViewModel(clip);

        vm.SyncModelFromEditor();

        // Table XML round-trips through the model, so it gets normalized
        Assert.Contains("BaseTable", vm.Clip.XmlData);
        Assert.Contains("name=\"T\"", vm.Clip.XmlData);
    }

    [Fact]
    public void ClipXml_UpdatesBothClipAndDocument()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");
        var vm = CreateScriptClip(xml);

        // Access XML document to create it
        _ = vm.XmlDocument;

        var newXml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>new</Text></Step>");
        vm.ClipXml = newXml;

        Assert.Equal(newXml, vm.Clip.XmlData);
        Assert.Equal(newXml, vm.XmlDocument.Text);
    }

    [Fact]
    public void Name_TwoWayBinding()
    {
        var vm = CreateScriptClip(WrapXml(""));
        string? changed = null;
        vm.PropertyChanged += (_, args) => changed = args.PropertyName;

        vm.Name = "Renamed";
        Assert.Equal("Renamed", vm.Clip.Name);
        Assert.Equal("Name", changed);
    }
}
