using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using SharpFM.Model.Parsing;
using SharpFM.Model.Schema;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Strategy for FileMaker table-shaped clipboard formats. <c>Mac-XMTB</c>
/// (Table) carries a <c>&lt;BaseTable&gt;</c> wrapper plus its fields;
/// <c>Mac-XMFD</c> (Field) is the field-only variant. Both round-trip through
/// <see cref="FmTable.FromXml"/> / <see cref="FmTable.ToXml"/>.
/// </summary>
public sealed class TableClipStrategy : IClipTypeStrategy
{
    /// <summary>Strategy instance for <c>Mac-XMTB</c> (Table).</summary>
    public static IClipTypeStrategy Table { get; } =
        new TableClipStrategy("Mac-XMTB", "Table", wrapsBaseTable: true);

    /// <summary>Strategy instance for <c>Mac-XMFD</c> (Field).</summary>
    public static IClipTypeStrategy Field { get; } =
        new TableClipStrategy("Mac-XMFD", "Field", wrapsBaseTable: false);

    private readonly bool _wrapsBaseTable;

    private TableClipStrategy(string formatId, string displayName, bool wrapsBaseTable)
    {
        FormatId = formatId;
        DisplayName = displayName;
        _wrapsBaseTable = wrapsBaseTable;
    }

    public string FormatId { get; }
    public string DisplayName { get; }

    public ClipParseResult Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", "input was empty", "empty xml");
        }

        XElement input;
        try
        {
            input = XElement.Parse(xml);
        }
        catch (XmlException ex)
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "malformed xml");
        }

        if (input.Name.LocalName != "fmxmlsnippet")
        {
            return Failure(
                ParseDiagnosticKind.UnsupportedClipType,
                "/" + input.Name.LocalName,
                $"expected <fmxmlsnippet>, found <{input.Name.LocalName}>",
                "unsupported root element");
        }

        FmTable table;
        try
        {
            table = FmTable.FromXml(xml);
        }
        catch (Exception ex)
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "failed to parse table");
        }

        var output = XElement.Parse(table.ToXml());
        var diagnostics = new List<ClipParseDiagnostic>(XmlRoundTripDiff.Compute(input, output));

        var report = diagnostics.Count == 0
            ? ClipParseReport.Empty
            : new ClipParseReport(diagnostics);

        return new ParseSuccess(new TableClipModel(table), report);
    }

    public string DefaultXml(string clipName) =>
        _wrapsBaseTable
            ? $"<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"{clipName}\"></BaseTable></fmxmlsnippet>"
            : "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";

    private static ParseFailure Failure(
        ParseDiagnosticKind kind, string location, string message, string reason)
    {
        return new ParseFailure(reason, new ClipParseReport(
        [
            new ClipParseDiagnostic(kind, ParseDiagnosticSeverity.Error, location, message),
        ]));
    }
}
