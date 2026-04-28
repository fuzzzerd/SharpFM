namespace SharpFM.Model.Parsing;

/// <summary>
/// Outcome of parsing a clip's XML against a registered strategy. Discriminated
/// so consumers must explicitly handle the "couldn't produce a model at all"
/// case (<see cref="ParseFailure"/>) rather than receiving a silent empty model.
/// Both branches carry a <see cref="ClipParseReport"/> describing what was lost.
/// </summary>
public abstract record ClipParseResult(ClipParseReport Report);

/// <summary>Parse produced a domain model. The report may still contain warnings.</summary>
public sealed record ParseSuccess(ClipModel Model, ClipParseReport Report) : ClipParseResult(Report);

/// <summary>
/// Parse could not produce a model (e.g. malformed XML, wrong root element,
/// unsupported clip type). <paramref name="Reason"/> is a short description;
/// the report contains the underlying diagnostics.
/// </summary>
public sealed record ParseFailure(string Reason, ClipParseReport Report) : ClipParseResult(Report);
