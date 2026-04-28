using System.Xml;
using System.Xml.Linq;
using SharpFM.Model.Parsing;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Fallback strategy for clip formats with no registered handler. Validates
/// XML well-formedness, preserves the body verbatim, and never claims
/// fidelity beyond "we held onto the bytes." Used by
/// <see cref="ClipTypeRegistry.For"/> when an unknown <c>Mac-XM*</c> id arrives.
/// </summary>
public sealed class OpaqueClipStrategy : IClipTypeStrategy
{
    public static OpaqueClipStrategy Instance { get; } = new();

    private OpaqueClipStrategy() { }

    /// <summary>Synthetic id used only by the fallback singleton; never registered with the registry.</summary>
    public string FormatId => "Mac-XM??";

    public string DisplayName => "Unknown";

    public ClipParseResult Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return ClipStrategyHelpers.Failure(
                ParseDiagnosticKind.XmlMalformed, "/", "input was empty", "empty xml");
        }

        try
        {
            XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            return ClipStrategyHelpers.Failure(
                ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "malformed xml");
        }

        return new ParseSuccess(new OpaqueClipModel(xml), ClipParseReport.Empty);
    }

    public string DefaultXml(string clipName) =>
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";
}
