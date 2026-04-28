using System.Xml;
using System.Xml.Linq;
using SharpFM.Model.Parsing;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Shared parsing primitives used by every <see cref="IClipTypeStrategy"/>
/// implementation: a one-line failure builder and the standard
/// <c>&lt;fmxmlsnippet&gt;</c> well-formedness gate.
/// </summary>
internal static class ClipStrategyHelpers
{
    /// <summary>Build a <see cref="ParseFailure"/> wrapping a single error-severity diagnostic.</summary>
    public static ParseFailure Failure(
        ParseDiagnosticKind kind, string location, string message, string reason) =>
        new(reason, new ClipParseReport(
        [
            new ClipParseDiagnostic(kind, ParseDiagnosticSeverity.Error, location, message),
        ]));

    /// <summary>
    /// Validate that <paramref name="xml"/> is non-empty, well-formed, and
    /// rooted at <c>&lt;fmxmlsnippet&gt;</c>. Returns true with the parsed
    /// <see cref="XElement"/> on success; false with a populated
    /// <paramref name="failure"/> on any of the three failure modes the
    /// strategies share.
    /// </summary>
    public static bool TryParseFmxmlsnippet(string xml, out XElement root, out ParseFailure failure)
    {
        root = null!;
        failure = null!;

        if (string.IsNullOrWhiteSpace(xml))
        {
            failure = Failure(ParseDiagnosticKind.XmlMalformed, "/", "input was empty", "empty xml");
            return false;
        }

        try
        {
            root = XElement.Parse(xml);
        }
        catch (XmlException ex)
        {
            failure = Failure(ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "malformed xml");
            return false;
        }

        if (root.Name.LocalName != "fmxmlsnippet")
        {
            failure = Failure(
                ParseDiagnosticKind.UnsupportedClipType,
                "/" + root.Name.LocalName,
                $"expected <fmxmlsnippet>, found <{root.Name.LocalName}>",
                "unsupported root element");
            return false;
        }

        return true;
    }
}
