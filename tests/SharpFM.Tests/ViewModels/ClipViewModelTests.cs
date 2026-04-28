using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ClipViewModelTests
{
    private static string WrapXml(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    private static ClipViewModel CreateScriptClip(string xml) =>
        new(Clip.FromXml("Test", "Mac-XMSS", xml));

    [Fact]
    public void IsScriptClip_TrueForXMSS()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"));
        Assert.True(vm.IsScriptClip);
    }

    [Fact]
    public void IsScriptClip_FalseForTable()
    {
        var vm = new ClipViewModel(Clip.FromXml(
            "Test", "Mac-XMTB", "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>"));
        Assert.False(vm.IsScriptClip);
    }

    [Fact]
    public void ScriptDocument_LazyCreated()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello</Text></Step>");
        var vm = CreateScriptClip(xml);

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

        var freshXml = vm.Editor.ToXml();
        Assert.Contains("modified", freshXml);
    }

    [Fact]
    public void Replace_UpdatesScriptDocument()
    {
        var xml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>original</Text></Step>");
        var vm = CreateScriptClip(xml);

        _ = vm.ScriptDocument;

        var newXml = WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>changed via xml</Text></Step>");
        vm.Replace(newXml);

        Assert.Contains("changed via xml", vm.ScriptDocument!.Text);
    }

    [Fact]
    public void Replace_TableClip_RoundTripsXml()
    {
        var vm = new ClipViewModel(Clip.FromXml(
            "Test", "Mac-XMTB",
            "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"T\"></BaseTable></fmxmlsnippet>"));

        vm.Replace(vm.Clip.Xml);

        Assert.Contains("BaseTable", vm.Clip.Xml);
        Assert.Contains("name=\"T\"", vm.Clip.Xml);
    }

    [Fact]
    public void Replace_UpdatesXmlDocumentForFallbackClip()
    {
        var vm = new ClipViewModel(Clip.FromXml(
            "Test", "Mac-XML2", "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>"));

        var newXml = "<fmxmlsnippet type=\"FMObjectList\"><Layout name=\"Hello\"/></fmxmlsnippet>";
        vm.Replace(newXml);

        Assert.Contains("Hello", vm.Clip.Xml);
    }

    [Fact]
    public void Rename_ProducesRenamedAggregate()
    {
        var vm = CreateScriptClip(WrapXml(""));
        var renamed = vm.Clip.Rename("Renamed");

        Assert.Equal("Renamed", renamed.Name);
        Assert.Equal(vm.Clip.Xml, renamed.Xml);
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
    public void Replace_ResetsIsDirty()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>a</Text></Step>"));
        vm.ScriptDocument!.Text += "\n# edited";
        Assert.True(vm.IsDirty);

        vm.Replace(vm.Editor.ToXml());
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ParseReport_ReflectsClipParseState()
    {
        var vm = CreateScriptClip(WrapXml("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>"));
        Assert.True(vm.IsLossless);
    }
}
