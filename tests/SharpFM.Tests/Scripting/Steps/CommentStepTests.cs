using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Tests for the typed CommentStep POCO. Comments are single-step entities
/// in FM Pro's XML (one Step element, Text may contain embedded newlines).
/// SharpFM renders them as a single display line with embedded newlines
/// substituted by the visible ⏎ glyph so the editor never splits a comment
/// across multiple physical lines.
/// </summary>
public class CommentStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string SingleLineXml =
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\">"
        + "<Text>this is a single line comment.</Text>"
        + "</Step>";

    private const string MultiLineXml =
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\">"
        + "<Text>this is\nmany\nlines</Text>"
        + "</Step>";

    private const string EmptyTextXml =
        "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text></Text></Step>";

    [Fact]
    public void SingleLine_Display_IsSingleHashLine()
    {
        var step = ScriptStep.FromXml(MakeStep(SingleLineXml));
        Assert.Equal("# this is a single line comment.", step.ToDisplayLine());
    }

    [Fact]
    public void MultiLine_Display_UsesReturnGlyphNotRealNewline()
    {
        var step = ScriptStep.FromXml(MakeStep(MultiLineXml));
        var display = step.ToDisplayLine();

        // The point of the ⏎ glyph is: display must be a SINGLE line
        // regardless of internal newlines in the Text.
        Assert.DoesNotContain('\n', display);
        Assert.Equal("# this is\u23CEmany\u23CElines", display);
    }

    [Fact]
    public void MultiLine_RoundTrip_PreservesEmbeddedNewlines()
    {
        var step = ScriptStep.FromXml(MakeStep(MultiLineXml));
        var xml = step.ToXml();

        Assert.Equal("this is\nmany\nlines", xml.Element("Text")!.Value);
    }

    [Fact]
    public void FullRoundTrip_MultiLine_XmlBytesPreserved()
    {
        // XML → display → XML must preserve Text exactly.
        var step1 = ScriptStep.FromXml(MakeStep(MultiLineXml));
        var display = step1.ToDisplayLine();
        var step2 = ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        Assert.Equal("this is\nmany\nlines", xml.Element("Text")!.Value);
    }

    [Fact]
    public void CarriageReturn_InCommentText_DoesNotLeakIntoDisplay()
    {
        // FM Pro clipboard commonly uses &#13; (\r) as the comment-text
        // line separator. Those CR chars must not leak into the display
        // document — AvaloniaEdit would treat them as visual line breaks,
        // and MultiLineStatementRanges (which splits on \n only) would
        // disagree with the editor on line numbering, causing highlight
        // rectangles to span what look like unrelated steps.
        var xml =
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\">"
            + "<Text>Description: \r\rBody line with CR-only line breaks.\rAnother.</Text>"
            + "</Step>";

        var step = ScriptStep.FromXml(MakeStep(xml));
        var display = step.ToDisplayLine();

        Assert.DoesNotContain('\r', display);
        Assert.DoesNotContain('\n', display);
    }

    [Fact]
    public void CarriageReturn_RoundTrip_NormalizesToNewline()
    {
        // Round-trip of \r-carrying text. The CR normalizes to \n in the
        // POCO's Text (and therefore in re-emitted XML) — acceptable
        // normalization because FM Pro treats CR/LF equivalently for
        // comment content. Critical property: no raw newlines survive
        // in the display-text layer.
        var xml =
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\">"
            + "<Text>a\rb\r\nc\nd</Text>"
            + "</Step>";

        var step1 = ScriptStep.FromXml(MakeStep(xml));
        var display = step1.ToDisplayLine();
        Assert.DoesNotContain('\r', display);
        Assert.DoesNotContain('\n', display);

        var step2 = ScriptTextParser.FromDisplayLine(display);
        var outText = step2.ToXml().Element("Text")!.Value;

        // All line endings normalized to \n; each original break becomes one \n.
        Assert.Equal("a\nb\nc\nd", outText);
    }

    [Fact]
    public void EmptyText_Display_IsBlankLine()
    {
        // FM Pro convention: a <Step id="89"> with empty Text renders as
        // a blank line in the script editor. SharpFM matches: empty
        // comment text → empty display string (no leading '#'). The
        // parse side restores this by treating any blank display line as
        // an empty comment step.
        var step = ScriptStep.FromXml(MakeStep(EmptyTextXml));
        Assert.Equal(string.Empty, step.ToDisplayLine());
    }

    [Fact]
    public void BareCommentStep_NoTextElement_DisplayIsBlankLine()
    {
        // Some source XML omits the <Text> element entirely when the
        // comment is blank. Treat identical to empty text.
        var xml = "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"></Step>";
        var step = ScriptStep.FromXml(MakeStep(xml));
        Assert.Equal(string.Empty, step.ToDisplayLine());
    }

    [Fact]
    public void BlankDisplayLine_ParsesToEmptyCommentStep()
    {
        // Round-trip counterpart of the blank-line rendering: a blank
        // line in the display text becomes an empty CommentStep on parse.
        // A lone "\n" is two empty-comment steps because '\n' separates
        // two blank lines in the split; see the FM Pro model notes.
        var script = SharpFM.Scripting.ScriptTextParser.FromDisplayText("\n");

        Assert.Equal(2, script.Steps.Count);
        foreach (var s in script.Steps)
        {
            var cs = Assert.IsType<CommentStep>(s);
            Assert.Equal("", cs.Text);
        }
    }

    [Fact]
    public void InterspersedBlankLines_ProduceEmptyCommentSteps()
    {
        // Realistic: two real steps with a blank between them. Three
        // script steps in the round-trip (matches FM Pro's model).
        var display = "If [ $x > 0 ]\n\nEnd If";
        var script = SharpFM.Scripting.ScriptTextParser.FromDisplayText(display);

        Assert.Equal(3, script.Steps.Count);
        Assert.IsType<IfStep>(script.Steps[0]);
        var middle = Assert.IsType<CommentStep>(script.Steps[1]);
        Assert.Equal("", middle.Text);
        Assert.IsType<EndIfStep>(script.Steps[2]);
    }

    [Fact]
    public void SingleEmptyComment_Snippet_DoesNotCrashOnLoad()
    {
        // Regression: pasting a clip that consists of a single empty
        // comment step would crash. Covers the ScriptClipEditor ctor +
        // anchor-cache build path for this minimal-but-valid input.
        var xml = "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"></Step>"
            + "</fmxmlsnippet>";
        var editor = new SharpFM.Editors.ScriptClipEditor(xml);
        var xmlOut = editor.ToXml();

        // Parseable; empty comment survives the round-trip.
        var doc = System.Xml.Linq.XDocument.Parse(xmlOut);
        var steps = doc.Root!.Elements("Step").ToArray();
        Assert.Single(steps);
        Assert.Equal("# (comment)", steps[0].Attribute("name")!.Value);
    }

    [Fact]
    public void DisplayText_FromSingleLine_ParsesToCommentStep()
    {
        var step = ScriptTextParser.FromDisplayLine("# hello world");

        var typed = Assert.IsType<CommentStep>(step);
        Assert.Equal("hello world", typed.Text);
    }

    [Fact]
    public void DisplayText_WithReturnGlyph_ParsesMultiLineText()
    {
        var step = ScriptTextParser.FromDisplayLine("# foo\u23CEbar\u23CEbaz");

        var typed = Assert.IsType<CommentStep>(step);
        Assert.Equal("foo\nbar\nbaz", typed.Text);
    }

    [Fact]
    public void FullRoundTrip_Script_WithMixedComments_Preserves()
    {
        // Realistic: single-line comment, some real step, multi-line comment.
        var xml =
            "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>header</Text></Step>"
            + "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>line1\nline2\nline3</Text></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"></Step>"
            + "</fmxmlsnippet>";

        var script1 = FmScript.FromXml(xml);
        var display = script1.ToDisplayText();
        var script2 = ScriptTextParser.FromDisplayText(display);

        // The multi-line comment must still be exactly ONE step after round-trip.
        Assert.Equal(4, script2.Steps.Count);
        Assert.Equal("# (comment)", script2.Steps[0].Definition?.Name);
        Assert.Equal("# (comment)", script2.Steps[2].Definition?.Name);

        var roundTrippedMultiLine = Assert.IsType<CommentStep>(script2.Steps[2]);
        Assert.Equal("line1\nline2\nline3", roundTrippedMultiLine.Text);
    }
}
