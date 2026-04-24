using SharpFM.Model;
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
    public void EditorToXml_UpdatesClipXml()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>original</Text></Step>");
        var vm = CreateScriptClip(xml);

        var doc = vm.ScriptDocument;
        doc!.Text = "# modified";

        // Editor.ToXml() gives fresh XML from current editor state
        var freshXml = vm.Editor.ToXml();
        Assert.Contains("modified", freshXml);
    }

    [Fact]
    public void ReplaceEditor_UpdatesScriptDocument()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>original</Text></Step>");
        var vm = CreateScriptClip(xml);

        _ = vm.ScriptDocument;

        var newXml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>changed via xml</Text></Step>");
        vm.ReplaceEditor(newXml);

        Assert.Contains("changed via xml", vm.ScriptDocument!.Text);
    }

    [Fact]
    public void ReplaceEditor_TableClip_RoundTripsXml()
    {
        var clip = new FileMakerClip("Test", "Mac-XMTB", "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"T\"></BaseTable></fmxmlsnippet>");
        var vm = new ClipViewModel(clip);

        vm.ReplaceEditor(vm.Clip.XmlData);

        Assert.Contains("BaseTable", vm.Clip.XmlData);
        Assert.Contains("name=\"T\"", vm.Clip.XmlData);
    }

    [Fact]
    public void Clip_XmlData_UpdatesBothClipAndDocument()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");
        var vm = CreateScriptClip(xml);

        // Access XML document to create it
        _ = vm.XmlDocument;

        var newXml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>new</Text></Step>");
        vm.Clip.XmlData = newXml;

        Assert.Equal(newXml, vm.Clip.XmlData);
        Assert.Equal(newXml, vm.XmlDocument.Text);
    }

    [Fact]
    public void Clip_Name_FiresPropertyChanged()
    {
        var vm = CreateScriptClip(WrapXml(""));
        string? changed = null;
        vm.Clip.PropertyChanged += (_, args) => changed = args.PropertyName;

        vm.Clip.Name = "Renamed";
        Assert.Equal("Renamed", vm.Clip.Name);
        Assert.Equal("Name", changed);
    }

    [Fact]
    public void IsDirty_FalseImmediatelyAfterConstruction()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterEditorEdit()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step>"));
        vm.ScriptDocument!.Text += "\n# new line";

        // IsDirty is computed live — no need to pump the ContentChanged
        // debouncer; a UI binding watching IsDirty gets notified when
        // ContentChanged fires, but the value itself is always fresh.
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void MarkSaved_ClearsIsDirty()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step>"));
        vm.ScriptDocument!.Text += "\n# edited";
        Assert.True(vm.IsDirty);

        vm.MarkSaved();
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ReplaceEditor_ResetsIsDirty()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step>"));
        vm.ScriptDocument!.Text += "\n# edited";
        Assert.True(vm.IsDirty);

        vm.ReplaceEditor(vm.Editor.ToXml());
        Assert.False(vm.IsDirty);
    }
}
