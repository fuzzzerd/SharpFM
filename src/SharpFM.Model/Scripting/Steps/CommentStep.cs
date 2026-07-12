using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// <c># (comment)</c> script step. A comment's <c>Text</c> may contain
/// embedded newlines; the display form substitutes each <c>\n</c> with a
/// visible <c>⏎</c> (U+23CE) return glyph so the comment stays on a
/// single editor line. The glyph is reversed to a real newline on parse.
/// </summary>
public sealed class CommentStep : ScriptStep<CommentStep>, IStepFactory
{
    public const int XmlId = 89;
    public const string XmlName = "# (comment)";

    /// <summary>Canonical separator in the display form. Authored newlines
    /// in the XML <c>Text</c> element are substituted with this glyph when
    /// rendering display text, and reversed when parsing.</summary>
    public const char ReturnGlyph = '\u23CE';

    public string Text { get; set; }

    private CommentStep() : base(false) { Text = ""; }

    public CommentStep(bool enabled, string text)
        : base(enabled)
    {
        Text = text;
    }

    // Hand-written: normalizes the clipboard's CR/CRLF newlines to LF, a
    // text transform the shape engine does not express.
    protected internal override void PopulateFromXml(XElement step)
    {
        // Normalize all line endings to \n. FM Pro's clipboard XML uses
        // &#13; (CR) as its canonical newline; those must not leak into
        // the display document because AvaloniaEdit would treat them as
        // visual line breaks, desynchronizing the editor's line numbers
        // from MultiLineStatementRanges (which splits on \n only).
        var raw = step.Element("Text")?.Value ?? "";
        Text = NormalizeNewlines(raw);
    }

    // Hand-written: the comment grammar (# prefix, no brackets) is outside
    // the shared "Name [ tokens ]" display form.
    public override string ToDisplayLine() =>
        // FM Pro convention: an empty-text comment renders as a blank
        // line in the script editor, not "# ". Blank display lines are
        // round-tripped back to empty CommentSteps by ScriptTextParser.
        string.IsNullOrEmpty(Text)
            ? string.Empty
            : "# " + Text.Replace("\n", ReturnGlyph.ToString());

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        // ScriptLineParser routes a comment through as a single hrParam
        // containing everything after the '#' (TrimStart'd). Restore any
        // ⏎ glyphs back to real newlines before storing.
        var raw = hrParams.Length > 0 ? hrParams[0] : "";
        Text = raw.Replace(ReturnGlyph.ToString(), "\n");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        // Canonical (skill §8.1): a bare divider comment is the self-closing
        // form, not an empty <Text/> — hence Optional.
        Shape =
        [
            new NamedTextChild("Text") { Optional = true },
        ],
    };

    // CR/LF/CRLF → LF. Critical because any raw CR leaking into the
    // display document would be interpreted as a visual line break by
    // AvaloniaEdit while MultiLineStatementRanges can't see it (it only
    // splits on LF), causing highlight/indent/margin mismatches.
    private static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");
}
