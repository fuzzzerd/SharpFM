using System;
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

    public string FormatId => "Mac-XM??";
    public string DisplayName => "Unknown";

    public ClipParseResult Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new ParseFailure("empty xml", new ClipParseReport(
            [
                new ClipParseDiagnostic(
                    ParseDiagnosticKind.XmlMalformed,
                    ParseDiagnosticSeverity.Error,
                    "/",
                    "input was empty"),
            ]));
        }

        try
        {
            XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            return new ParseFailure("malformed xml", new ClipParseReport(
            [
                new ClipParseDiagnostic(
                    ParseDiagnosticKind.XmlMalformed,
                    ParseDiagnosticSeverity.Error,
                    "/",
                    ex.Message),
            ]));
        }

        return new ParseSuccess(new OpaqueClipModel(xml), ClipParseReport.Empty);
    }

    public string DefaultXml(string clipName) =>
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";
}
