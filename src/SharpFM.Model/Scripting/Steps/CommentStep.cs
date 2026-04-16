using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's <c># (comment)</c> script
/// step. A comment is always a single <c>Step</c> element whose
/// <c>Text</c> may contain embedded newlines. SharpFM's display keeps
/// the comment on a single editor line by substituting each <c>\n</c>
/// with the visible <c>⏎</c> (U+23CE) return glyph — users never see
/// a multi-line comment split across physical editor lines.
/// </summary>
public sealed class CommentStep : ScriptStep
{
    /// <summary>Canonical separator in the display form. Authored newlines
    /// in the XML <c>Text</c> element are substituted with this glyph when
    /// rendering display text, and reversed when parsing.</summary>
    public const char ReturnGlyph = '\u23CE';

    public string Text { get; set; }

    public CommentStep(bool enabled, string text)
        : base(StepCatalogLoader.ByName["# (comment)"], enabled)
    {
        Text = text;
    }

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register typed step factories on assembly load.")]
    [ModuleInitializer]
    internal static void Register()
    {
        StepXmlFactory.Register("# (comment)", FromXml);
        StepDisplayFactory.Register("# (comment)", FromDisplayParams);
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
            new XAttribute("id", 89),
            new XAttribute("name", "# (comment)"),
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

    // CR/LF/CRLF → LF. Critical because any raw CR leaking into the
    // display document would be interpreted as a visual line break by
    // AvaloniaEdit while MultiLineStatementRanges can't see it (it only
    // splits on LF), causing highlight/indent/margin mismatches.
    private static string NormalizeNewlines(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");
}
