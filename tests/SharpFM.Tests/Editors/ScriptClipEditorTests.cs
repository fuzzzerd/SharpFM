using SharpFM.Editors;
using Xunit;

namespace SharpFM.Tests.Editors;

public class ScriptClipEditorTests
{
    private const string SampleScriptXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"89\" name=\"Comment\">" +
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
    public void IsDirty_TracksUnsavedChanges()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        Assert.False(editor.IsDirty);

        editor.Document.Text = "# modified comment";
        Assert.True(editor.IsDirty);

        editor.Save();
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void Save_UpdatesModel()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        editor.Document.Text = "# new comment";
        editor.Save();

        var xml = editor.ToXml();
        Assert.Contains("new comment", xml);
    }

    [Fact]
    public void ToXml_ReturnsSavedState()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        var original = editor.ToXml();

        editor.Document.Text = "# unsaved edit";

        // ToXml returns saved state, not live buffer
        Assert.Equal(original, editor.ToXml());
    }

    [Fact]
    public void FromXml_ClearsDirtyState()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        editor.Document.Text = "# dirty";
        Assert.True(editor.IsDirty);

        editor.FromXml(SampleScriptXml);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void Script_ExposesModel()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);
        Assert.NotNull(editor.Script);
        Assert.NotEmpty(editor.Script.Steps);
    }
}
