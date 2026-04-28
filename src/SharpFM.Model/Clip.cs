using System;
using System.Linq;
using System.Text;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;

namespace SharpFM.Model;

/// <summary>
/// Immutable aggregate for a FileMaker clip: name, wire-format identifier,
/// canonical XML body, and the parsed domain model + fidelity report. Every
/// path that ingests a clip (paste from FileMaker, file load, plugin push,
/// new-clip seed) ends up here, so the parse report is always available to
/// any consumer regardless of how the clip arrived.
/// </summary>
/// <remarks>
/// Mutation produces a new instance: <see cref="WithXml"/> reparses against
/// the registered strategy; <see cref="Rename"/> reuses the existing parse.
/// View-models hold a <c>Clip</c> reference and re-publish change
/// notifications when the reference is replaced — INPC stays out of the
/// domain layer.
/// </remarks>
public sealed class Clip
{
    /// <summary>Display name of the clip.</summary>
    public string Name { get; }

    /// <summary>Wire-format identifier, e.g. <c>"Mac-XMSS"</c>.</summary>
    public string FormatId { get; }

    /// <summary>Canonical (pretty-printed) XML body. Always retains whatever the source produced for unknown content.</summary>
    public string Xml { get; }

    /// <summary>Outcome of parsing <see cref="Xml"/> against the registered strategy for <see cref="FormatId"/>.</summary>
    public ClipParseResult Parsed { get; }

    private byte[]? _cachedWireBytes;

    /// <summary>
    /// FileMaker clipboard wire format: 4-byte little-endian length prefix
    /// followed by UTF-8 XML. Lazily derived from <see cref="Xml"/>.
    /// </summary>
    public byte[] WireBytes
    {
        get
        {
            if (_cachedWireBytes is null)
            {
                var payload = Encoding.UTF8.GetBytes(Xml);
                var prefix = BitConverter.GetBytes(payload.Length);
                _cachedWireBytes = prefix.Concat(payload).ToArray();
            }
            return _cachedWireBytes;
        }
    }

    private Clip(string name, string formatId, string xml, ClipParseResult parsed)
    {
        Name = name;
        FormatId = formatId;
        Xml = xml;
        Parsed = parsed;
    }

    /// <summary>
    /// Construct a clip from raw XML. The XML is canonicalised via
    /// <see cref="XmlHelpers.PrettyPrint"/> (which preserves the input on
    /// well-formedness errors), then handed to the registered strategy for
    /// <paramref name="formatId"/>. Parse failures are returned in
    /// <see cref="Parsed"/>; this method itself does not throw.
    /// </summary>
    public static Clip FromXml(string name, string formatId, string xml)
    {
        var canonical = XmlHelpers.PrettyPrint(xml ?? string.Empty);
        var strategy = ClipTypeRegistry.For(formatId);
        var parsed = strategy.Parse(canonical);
        return new Clip(name, formatId, canonical, parsed);
    }

    /// <summary>
    /// Construct a clip from FileMaker clipboard wire bytes (4-byte length
    /// prefix + UTF-8 XML). Inputs shorter than 4 bytes are treated as empty.
    /// </summary>
    public static Clip FromWireBytes(string name, string formatId, byte[] bytes)
    {
        var xml = bytes.Length < 4
            ? string.Empty
            : Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4);
        return FromXml(name, formatId, xml);
    }

    /// <summary>Return a fresh clip with replacement XML. Re-parses against the registered strategy.</summary>
    public Clip WithXml(string newXml) => FromXml(Name, FormatId, newXml);

    /// <summary>Return a fresh clip with a new name; parse state is reused since XML is unchanged.</summary>
    public Clip Rename(string newName) => new(newName, FormatId, Xml, Parsed);
}
