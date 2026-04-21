using SharpFM.Editors;
using Xunit;

namespace SharpFM.Tests.Editors;

public class ScriptClipEditorTests
{
    private const string SampleScriptXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\">" +
        "<Text>Hello</Text></Step></fmxmlsnippet>";

    [Fact]
    public void Constructor_ParsesXmlToDisplayText()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);

        Assert.NotNull(editor.Document);
        // Comment step renders as "# Hello" in display text
        Assert.Contains("Hello", editor.Document.Text);
    }

    [Fact]
    public void ToXml_RoundTrips()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        var xml = editor.ToXml();

        Assert.Contains("fmxmlsnippet", xml);
        Assert.Contains("Hello", xml);
        Assert.False(editor.IsPartial);
    }

    [Fact]
    public void FromXml_UpdatesDocument()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        var originalText = editor.Document.Text;

        var newXml =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<Step enable=\"True\" id=\"93\" name=\"Beep\"/></fmxmlsnippet>";
        editor.FromXml(newXml);

        Assert.NotEqual(originalText, editor.Document.Text);
        Assert.Contains("Beep", editor.Document.Text);
    }

    [Fact]
    public void ToXml_EmptyScript_IsNotPartial()
    {
        var editor = new ScriptClipEditor("<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>");
        var xml = editor.ToXml();

        Assert.False(editor.IsPartial);
        Assert.Contains("fmxmlsnippet", xml);
    }

    [Fact]
    public void Constructor_HandlesNullXml()
    {
        var editor = new ScriptClipEditor(null);

        Assert.NotNull(editor.Document);
        Assert.NotNull(editor.ToXml());
    }

    [Fact]
    public void UnknownStep_IsSealedForXmlEditorOnly()
    {
        // A step whose name isn't in the registry becomes a RawStep; the
        // editor seals its display line so display-text edits can't
        // silently corrupt the preserved XML. The cog icon / XML editor
        // handles changes.
        var xml =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"><Foo>bar</Foo></Step>" +
            "</fmxmlsnippet>";
        var editor = new ScriptClipEditor(xml);

        Assert.Single(editor.SealedAnchors);

        // Round-trip: the unknown element survives byte-intact through
        // the editor even though it's just a line of display text.
        var emitted = editor.ToXml();
        Assert.Contains("<Foo>bar</Foo>", emitted);
        Assert.Contains("FutureStep", emitted);
    }
}
