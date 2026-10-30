using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SharpFM.Diagnostics;

internal sealed record ClipboardXmlProbe(
    int Offset, string Utf8Status, string XmlStatus,
    string? RootName = null, string? SnippetType = null, string? Detail = null, string? Bom = null);

internal sealed record ClipboardPayloadInterpretation(
    string LeadingHex, string Bom, uint? LittleEndianLength,
    bool? LengthMatchesRemainingBytes, IReadOnlyList<ClipboardXmlProbe> XmlHypotheses);

/// <summary>Independent, non-destructive probes. Never invokes the normal clip decoder.</summary>
internal static class ClipboardPayloadInterpreter
{
    public const int MaxProbeBytes = 2 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static ClipboardPayloadInterpretation Interpret(byte[] bytes)
    {
        uint? length = bytes.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : null;
        return new ClipboardPayloadInterpretation(
            Convert.ToHexString(bytes.AsSpan(0, Math.Min(32, bytes.Length))),
            Bom(bytes), length, length.HasValue ? length.Value == (long)bytes.Length - 4 : null,
            [Probe(bytes, 0), Probe(bytes, 4)]);
    }

    private static ClipboardXmlProbe Probe(byte[] bytes, int offset) =>
        ProbeXml(bytes, offset) with { Bom = offset <= bytes.Length ? Bom(bytes.AsSpan(offset)) : null };

    private static ClipboardXmlProbe ProbeXml(byte[] bytes, int offset)
    {
        if (offset > bytes.Length)
            return new(offset, "Unavailable", "NotAttempted", Detail: "Offset exceeds payload length.");
        if (bytes.Length - offset > MaxProbeBytes)
            return new(offset, "Skipped", "NotAttempted", Detail: $"Probe exceeds {MaxProbeBytes} bytes; full payload retained.");

        string text;
        try
        {
            text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            return new(offset, "Invalid", "NotAttempted", Detail: "Not strict UTF-8.");
        }

        try
        {
            // A byte-order mark is an encoding marker, not XML content. Only the
            // probe accepts it; the captured bytes remain untouched.
            using var input = new StringReader(text.StartsWith('\uFEFF') ? text[1..] : text);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxProbeBytes,
                ConformanceLevel = ConformanceLevel.Document
            });
            string? root = null;
            string? snippetType = null;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
                {
                    root = Abbreviate(reader.Name);
                    if (reader.LocalName == "fmxmlsnippet") snippetType = Abbreviate(reader.GetAttribute("type"));
                }
            }
            return root is null
                ? new(offset, "Valid", "Invalid", Detail: "XML has no root element.")
                : new(offset, "Valid", "Valid", root, snippetType);
        }
        catch (XmlException ex)
        {
            return new(offset, "Valid", "Invalid", Detail: $"XML rejected at line {ex.LineNumber}, position {ex.LinePosition}; DTDs are prohibited.");
        }
    }

    private static string? Abbreviate(string? value) =>
        value?.Length > 200 ? value[..200] + "… (summary truncated; full payload retained)" : value;

    private static string Bom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) return "UTF-8";
        if (bytes.StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 })) return "UTF-32 LE";
        if (bytes.StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF })) return "UTF-32 BE";
        if (bytes.StartsWith(new byte[] { 0xFF, 0xFE })) return "UTF-16 LE";
        if (bytes.StartsWith(new byte[] { 0xFE, 0xFF })) return "UTF-16 BE";
        return "None";
    }
}
