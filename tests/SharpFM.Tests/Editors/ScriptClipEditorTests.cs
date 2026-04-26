using System.Linq;
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

    private const string OneSealedAtLineTwoXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>before</Text></Step>" +
        "<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"><Foo>bar</Foo></Step>" +
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>after</Text></Step>" +
        "</fmxmlsnippet>";

    [Fact]
    public void SealedLineNumbers_PointsAtSealedStepLine()
    {
        // The sealed FutureStep is the second display line; the
        // colorizer / cog generator / sealed-step layer all read this
        // set per paint and must see line 2.
        var editor = new ScriptClipEditor(OneSealedAtLineTwoXml);

        Assert.Single(editor.SealedLineNumbers);
        Assert.Contains(2, editor.SealedLineNumbers);
    }

    [Fact]
    public void SealedLineEndOffsets_KeyedByLineNumber()
    {
        var editor = new ScriptClipEditor(OneSealedAtLineTwoXml);
        var line2 = editor.Document.GetLineByNumber(2);

        Assert.Single(editor.SealedLineEndOffsets);
        Assert.Equal(line2.EndOffset, editor.SealedLineEndOffsets[2]);
    }

    [Fact]
    public void SealedLineNumbers_Empty_WhenNoSealedSteps()
    {
        var editor = new ScriptClipEditor(SampleScriptXml);

        Assert.Empty(editor.SealedLineNumbers);
        Assert.Empty(editor.SealedLineEndOffsets);
    }

    [Fact]
    public void SealedLineNumbers_RecomputesAfterDocumentEdit()
    {
        // Inserting a fresh line above the sealed step should slide its
        // line number from 2 → 3. Without cache invalidation the
        // dimming colorizer would highlight the wrong physical line.
        var editor = new ScriptClipEditor(OneSealedAtLineTwoXml);
        Assert.Contains(2, editor.SealedLineNumbers);

        editor.Document.Insert(0, "# new top line\n");

        Assert.Contains(3, editor.SealedLineNumbers);
        Assert.DoesNotContain(2, editor.SealedLineNumbers);
        var line3 = editor.Document.GetLineByNumber(3);
        Assert.Equal(line3.EndOffset, editor.SealedLineEndOffsets[3]);
    }

    [Fact]
    public void SealedLineNumbers_DropsEntryWhenLineDeleted()
    {
        // Deleting the entire sealed line drops the anchor (its
        // SurviveDeletion flag is false), so the cache should empty
        // out on next read.
        var editor = new ScriptClipEditor(OneSealedAtLineTwoXml);
        var sealedLine = editor.Document.GetLineByNumber(2);
        Assert.Contains(2, editor.SealedLineNumbers);

        // Delete sealed line including its trailing newline
        editor.Document.Remove(sealedLine.Offset, sealedLine.TotalLength);
        editor.ToXml(); // forces RebuildFromDocument → prunes dead anchors

        Assert.Empty(editor.SealedLineNumbers);
    }
}
