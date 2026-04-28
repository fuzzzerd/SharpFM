using SharpFM.Model.Parsing;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Strategy for the FileMaker layout clipboard format <c>Mac-XML2</c>.
/// SharpFM does not yet model layouts, so the strategy parses for
/// well-formedness and a sane root element but preserves the body verbatim
/// in <see cref="LayoutClipModel"/>. Promoting layouts to a typed domain
/// model is a future change; this strategy makes that promotion drop-in.
/// </summary>
public sealed class LayoutClipStrategy : IClipTypeStrategy
{
    public static IClipTypeStrategy Instance { get; } = new LayoutClipStrategy();

    private LayoutClipStrategy() { }

    public string FormatId => "Mac-XML2";
    public string DisplayName => "Layout";

    public ClipParseResult Parse(string xml)
    {
        if (!ClipStrategyHelpers.TryParseFmxmlsnippet(xml, out _, out var failure))
        {
            return failure;
        }
        return new ParseSuccess(new LayoutClipModel(xml), ClipParseReport.Empty);
    }

    public string DefaultXml(string clipName) =>
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";
}
