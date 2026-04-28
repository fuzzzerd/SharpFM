using System;
using System.Linq;
using System.Text;
using System.Threading;
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
/// View-models hold a <c>Clip</c> reference and re-publish change
/// notifications when the reference is replaced — INPC stays out of the
/// domain layer.
/// </remarks>
public sealed class Clip
{
    public string Name { get; }
    public string FormatId { get; }
    public string Xml { get; }

    private readonly Func<ClipParseResult> _parseFactory;
    private ClipParseResult? _parsed;

    /// <summary>
    /// Outcome of parsing <see cref="Xml"/> against the registered strategy.
    /// Computed on first access — XML→domain parsing is the slow part of
    /// constructing a clip, and most callers along the editor's hot path
    /// only read <see cref="Xml"/>.
    /// </summary>
    public ClipParseResult Parsed
    {
        get
        {
            // PublicationOnly semantics: multiple racing threads may compute
            // the parse, only one result wins. Cheaper than full
            // double-checked locking and the strategies are pure functions.
            if (_parsed is null)
            {
                Interlocked.CompareExchange(ref _parsed, _parseFactory(), null);
            }
            return _parsed;
        }
    }

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

    private Clip(string name, string formatId, string xml, Func<ClipParseResult> parseFactory)
    {
        Name = name;
        FormatId = formatId;
        Xml = xml;
        _parseFactory = parseFactory;
    }

    private Clip(string name, string formatId, string xml, ClipParseResult parsed)
        : this(name, formatId, xml, () => parsed)
    {
        _parsed = parsed;
    }

    /// <summary>
    /// Construct a clip from raw XML. The XML is canonicalised via
    /// <see cref="XmlHelpers.PrettyPrint"/>; the strategy parse runs lazily
    /// when <see cref="Parsed"/> is first read. This method itself does not
    /// throw — well-formedness errors surface as a <see cref="ParseFailure"/>
    /// from the strategy on demand.
    /// </summary>
    public static Clip FromXml(string name, string formatId, string xml)
    {
        var canonical = XmlHelpers.PrettyPrint(xml ?? string.Empty);
        return new Clip(
            name,
            formatId,
            canonical,
            () => ClipTypeRegistry.For(formatId).Parse(canonical));
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

    /// <summary>
    /// Return a fresh clip with replacement XML. The parse runs lazily on
    /// the new instance. Returns <c>this</c> when <paramref name="newXml"/>
    /// is byte-identical to the current canonical XML, which short-circuits
    /// the change cascade for keystroke-driven re-syncs that don't actually
    /// change anything.
    /// </summary>
    public Clip WithXml(string newXml)
    {
        if (string.Equals(newXml, Xml, StringComparison.Ordinal))
        {
            return this;
        }
        return FromXml(Name, FormatId, newXml);
    }

    /// <summary>Return a fresh clip under a new name; the existing parse is reused.</summary>
    public Clip Rename(string newName)
    {
        var parsed = Parsed;
        return new Clip(newName, FormatId, Xml, parsed);
    }
}
