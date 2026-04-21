using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// <c># (comment)</c> script step. A comment's <c>Text</c> may contain
/// embedded newlines; the display form substitutes each <c>\n</c> with a
/// visible <c>⏎</c> (U+23CE) return glyph so the comment stays on a
/// single editor line. The glyph is reversed to a real newline on parse.
/// </summary>
public sealed class CommentStep : ScriptStep, IStepFactory
{
    public const int XmlId = 89;
    public const string XmlName = "# (comment)";

    /// <summary>Canonical separator in the display form. Authored newlines
    /// in the XML <c>Text</c> element are substituted with this glyph when
    /// rendering display text, and reversed when parsing.</summary>
    public const char ReturnGlyph = '\u23CE';

    public string Text { get; set; }

    public CommentStep(bool enabled, string text)
        : base(enabled)
    {
        Text = text;
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        // Normalize all line endings to \n. FM Pro's clipboard XML uses
        // &#13; (CR) as its canonical newline; those must not leak into
        // the display document because AvaloniaEdit would treat them as
        // visual line breaks, desynchronizing the editor's line numbers
        // from MultiLineStatementRanges (which splits on \n only).
        var raw = step.Element("Text")?.Value ?? "";
        var text = NormalizeNewlines(raw);
        return new CommentStep(enabled, text);
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Text", Text));

    public override string ToDisplayLine() =>
        // FM Pro convention: an empty-text comment renders as a blank
        // line in the script editor, not "# ". Blank display lines are
        // round-tripped back to empty CommentSteps by ScriptTextParser.
        string.IsNullOrEmpty(Text)
            ? string.Empty
            : "# " + Text.Replace("\n", ReturnGlyph.ToString());

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // ScriptLineParser routes a comment through as a single hrParam
        // containing everything after the '#' (TrimStart'd). Restore any
        // ⏎ glyphs back to real newlines before storing.
        var raw = hrParams.Length > 0 ? hrParams[0] : "";
        var text = raw.Replace(ReturnGlyph.ToString(), "\n");
        return new CommentStep(enabled, text);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Params =
        [
            new ParamMetadata { Name = "Text", XmlElement = "Text", Type = "text" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };

    // CR/LF/CRLF → LF. Critical because any raw CR leaking into the
    // display document would be interpreted as a visual line break by
    // AvaloniaEdit while MultiLineStatementRanges can't see it (it only
    // splits on LF), causing highlight/indent/margin mismatches.
    private static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");
}
