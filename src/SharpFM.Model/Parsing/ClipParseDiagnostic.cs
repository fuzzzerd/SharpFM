namespace SharpFM.Model.Parsing;

/// <summary>
/// A single XML→domain parse-fidelity issue discovered while parsing a clip.
/// Anchored to a location in the source XML so consumers can point a user
/// (or an agent authoring XML) at the exact spot.
/// </summary>
/// <param name="Kind">Category of the loss; consumers can group by this.</param>
/// <param name="Severity">Whether this is fatal, lossy, or informational.</param>
/// <param name="Location">An xpath-style locator into the source XML.</param>
/// <param name="Message">A human-readable description of the loss.</param>
public sealed record ClipParseDiagnostic(
    ParseDiagnosticKind Kind,
    ParseDiagnosticSeverity Severity,
    string Location,
    string Message);
