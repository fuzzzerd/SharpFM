using SharpFM.Model.Parsing;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Per-clip-type extension point. Each <c>Mac-XM*</c> format gets one
/// implementation registered with <see cref="ClipTypeRegistry"/>; everything
/// the host needs to know about that format (display name, parse, default
/// XML for a fresh clip) hangs off this interface.
/// </summary>
public interface IClipTypeStrategy
{
    /// <summary>The wire-format identifier this strategy handles, e.g. <c>"Mac-XMSS"</c>.</summary>
    string FormatId { get; }

    /// <summary>Human-readable label shown in the UI, e.g. <c>"Script Steps"</c>.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Parse <paramref name="xml"/> into a <see cref="ClipModel"/> and emit a
    /// <see cref="ClipParseReport"/> describing any data the parse couldn't
    /// represent in the domain model. Implementations must not throw — failures
    /// are returned as <see cref="ParseFailure"/>.
    /// </summary>
    ClipParseResult Parse(string xml);

    /// <summary>
    /// Produce a starter XML body for a fresh clip with the given name. Used by
    /// "new clip" flows in the host and by plugins.
    /// </summary>
    string DefaultXml(string clipName);
}
