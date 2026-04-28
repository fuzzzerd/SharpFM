namespace SharpFM.Model.Parsing;

/// <summary>
/// Categorises an XML→domain parse loss. Each <see cref="ClipParseDiagnostic"/>
/// carries one of these so consumers (UI, MCP tools, plugins) can surface the
/// loss in actionable form rather than as opaque text.
/// </summary>
public enum ParseDiagnosticKind
{
    /// <summary>Source XML was not well-formed or could not be parsed.</summary>
    XmlMalformed,

    /// <summary>The clip's format identifier has no registered strategy.</summary>
    UnsupportedClipType,

    /// <summary>A <c>&lt;Step&gt;</c> name is not in the step registry; preserved as a RawStep.</summary>
    UnknownStep,

    /// <summary>An attribute on a <c>&lt;Step&gt;</c> was not consumed by its parser.</summary>
    UnknownStepAttribute,

    /// <summary>A child element under a <c>&lt;Step&gt;</c> was not consumed by its parser.</summary>
    UnknownStepElement,

    /// <summary>An element under the clip root (e.g. <c>&lt;fmxmlsnippet&gt;</c>) was not consumed.</summary>
    UnknownClipElement,

    /// <summary>An attribute on the clip root was not consumed.</summary>
    UnknownClipAttribute,

    /// <summary>A namespace declaration in the source XML was not preserved through the round trip.</summary>
    DroppedNamespace,

    /// <summary>A modeled element parsed back with a value that differs from the input.</summary>
    RoundTripValueMismatch,
}
